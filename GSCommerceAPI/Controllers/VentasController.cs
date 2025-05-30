﻿using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Data;
using GSCommerce.Client.Models;
using GSCommerce.Client.Models.SUNAT;
using GSCommerceAPI.Services.SUNAT;
using GSCommerce.Client.Models.DTOs.Reportes;
using GSCommerceAPI.Models.SUNAT.DTOs;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VentasController : ControllerBase
    {
        private readonly SyscharlesContext _context;
        private readonly IFacturacionElectronicaService _facturacionService;

        public VentasController(SyscharlesContext context, IFacturacionElectronicaService facturacionService)
        {
            _context = context;
            _facturacionService = facturacionService;
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListarVentas([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var inicio = desde ?? DateTime.Today;
            var fin = hasta ?? DateTime.Today;

            var ventas = await _context.VVenta1s
                .Where(v => v.Fecha.Date >= inicio.Date && v.Fecha.Date <= fin.Date)
                .Select(v => new VentaConsultaDTO
                {
                    IdComprobante = v.IdComprobante,
                    TipoDocumento = v.IdTipoDocumento == 1 ? "BOLETA" :
                                    v.IdTipoDocumento == 2 ? "FACTURA" :
                                    v.IdTipoDocumento == 5 ? "BOLETA M" :
                                    v.IdTipoDocumento == 6 ? "FACTURA M" :
                                    v.IdTipoDocumento == 4 ? "TICKET" : "OTRO",
                    Serie = v.Serie,
                    Numero = v.Numero,
                    Fecha = v.Fecha,
                    NombreCliente = v.Nombre,
                    Total = v.Total,
                    Estado = v.Estado
                })
                .ToListAsync();

            return Ok(ventas);
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarVenta([FromBody] VentaRegistroDTO ventaRegistro)
        {
            if (ventaRegistro?.Cabecera == null || ventaRegistro.TipoDocumento == null || ventaRegistro.TipoDocumento.IdTipoDocumentoVenta == 0)
                return BadRequest("Falta seleccionar un tipo de documento válido.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            var nuevoNumero = await ObtenerNuevoNumeroSerieAsync(ventaRegistro.Cabecera.Serie);

            try
            {
                // Insertar cabecera
                var cabecera = new ComprobanteDeVentaCabecera
                {
                    IdTipoDocumento = ventaRegistro.TipoDocumento.IdTipoDocumentoVenta,
                    Serie = ventaRegistro.Cabecera.Serie,
                    Numero = nuevoNumero,
                    Fecha = DateTime.Now,
                    Nombre = ventaRegistro.Cabecera.NombreCliente,
                    Dniruc = ventaRegistro.Cabecera.DocumentoCliente,
                    Direccion = ventaRegistro.Cabecera.DireccionCliente,
                    TipoCambio = 1m, // Por ahora fijo, luego mejoramos
                    SubTotal = ventaRegistro.Cabecera.SubTotal,
                    Igv = ventaRegistro.Cabecera.Igv,
                    Total = ventaRegistro.Cabecera.Total,
                    Redondeo = 0,
                    Apagar = ventaRegistro.Cabecera.APagar,
                    IdVendedor = 1, // De momento fijo
                    IdCajero = 1, // De momento fijo
                    IdAlmacen = 1, // De momento fijo
                    Estado = "E",
                    FechaHoraRegistro = DateTime.Now,
                };

                _context.ComprobanteDeVentaCabeceras.Add(cabecera);
                await _context.SaveChangesAsync();  // Para generar el ID de la cabecera

                // Insertar detalles
                foreach (var detalle in ventaRegistro.Detalles)
                {
                    var nuevoDetalle = new ComprobanteDeVentaDetalle
                    {

                        IdComprobante = cabecera.IdComprobante,
                        Item = detalle.Item,
                        IdArticulo = detalle.CodigoItem,
                        Descripcion = detalle.DescripcionItem,
                        UnidadMedida = "UND", // Por ahora fijo
                        Cantidad = (int)detalle.Cantidad,
                        Precio = detalle.PrecioUnitario,
                        PorcentajeDescuento = detalle.PorcentajeDescuento,
                        Total = detalle.Total
                    };

                    _context.ComprobanteDeVentaDetalles.Add(nuevoDetalle);
                }

                // Insertar pagos
                foreach (var pago in ventaRegistro.Pagos)
                {
                    var nuevoPago = new DetallePagoVentum
                    {
                        IdComprobante = cabecera.IdComprobante,
                        IdTipoPagoVenta = pago.IdTipoPagoVenta,
                        Soles = pago.Soles,
                        Dolares = pago.Dolares, // De momento
                        Vuelto = pago.Vuelto,
                        Datos = null
                    };

                    _context.DetallePagoVenta.Add(nuevoPago);
                }

                await transaction.CommitAsync();
                await _context.SaveChangesAsync(); // Guardar detalles y pagos
                return Ok(new
                {
                    numero = cabecera.Numero,
                    idComprobante = cabecera.IdComprobante
                });
                /*return Ok(new
                {
                    success = true,
                    numero = nuevoNumero
                });*/

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"💥 Error general: {ex.Message}");
            }
        }

        // Métodos auxiliares

        /*
        private int MapearTipoDocumento(string tipo)
        {
            return tipo switch
            {
                "BOLETA" => 1,
                "FACTURA" => 2,
                "TICKET" => 4,
                _ => 1
            };
        }
        */
        [HttpPost("enviar-sunat")]
        public async Task<IActionResult> EnviarASUNAT([FromBody] GSCommerceAPI.Models.SUNAT.DTOs.ComprobanteCabeceraDTO cabecera)
        {
            try
            {
                // Aquí llamas a tu servicio de facturación electrónica (XML + firma + ZIP + envío SOAP)
                var resultado = await _facturacionService.EnviarComprobante(cabecera);
                if (cabecera.TipoDocumento == "TK")
                    return BadRequest("⚠️ El tipo de documento TICKET no se declara ante SUNAT.");
                if (resultado.exito)
                    return Ok("Enviado correctamente a SUNAT.");
                else
                    return BadRequest($"❌ Error: {resultado.mensaje}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"💥 Error: {ex.Message}");
            }
        }

        [HttpPost("enviar-resumen")]
        public async Task<IActionResult> EnviarResumenDiario([FromBody] List<GSCommerceAPI.Models.SUNAT.DTOs.ComprobanteCabeceraDTO> comprobantes)
        {
            var resultado = await _facturacionService.EnviarResumenDiario(comprobantes);
            return Ok(resultado);
        }

        [HttpGet("estado-sunat")]
        public async Task<IActionResult> ObtenerEstadosSunat([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        {
            var lista = await (from c in _context.ComprobanteDeVentaCabeceras
                               join s in _context.Comprobantes on c.IdComprobante equals s.IdComprobante into gj
                               from sub in gj.DefaultIfEmpty()
                               where c.Fecha.Date >= desde.Date && c.Fecha.Date <= hasta.Date
                                     && c.IdTipoDocumento != 4 // ❌ Excluir TICKET
                               select new EstadoSunatDTO
                               {
                                   IdComprobante = c.IdComprobante,
                                   TipoDocumento = c.IdTipoDocumento == 1 ? "BOLETA" :
                                                   c.IdTipoDocumento == 2 ? "FACTURA" :
                                                   c.IdTipoDocumento == 5 ? "BOLETA M" :
                                                   c.IdTipoDocumento == 6 ? "FACTURA M" :
                                                   "OTRO",
                                   Serie = c.Serie,
                                   Numero = c.Numero,
                                   FechaEmision = c.Fecha,
                                   EstadoSunat = sub == null
                                       ? "PENDIENTE"
                                       : sub.Estado
                                           ? "ACEPTADO"
                                           : "RECHAZADO",
                                   DescripcionSunat = sub != null ? sub.RespuestaSunat : null
                               }).ToListAsync();

            return Ok(lista);
        }

        private int MapearTipoPago(string descripcion)
        {
            if (descripcion.Contains("Efectivo", StringComparison.OrdinalIgnoreCase)) return 1;
            if (descripcion.Contains("Tarjeta", StringComparison.OrdinalIgnoreCase)) return 2;
            if (descripcion.Contains("Yape", StringComparison.OrdinalIgnoreCase)) return 3;
            if (descripcion.Contains("Transferencia", StringComparison.OrdinalIgnoreCase)) return 4;
            if (descripcion.Contains("Nota", StringComparison.OrdinalIgnoreCase)) return 5;

            return 1; // Por defecto Efectivo
        }

        [HttpGet("reporte-vendedor")]
        public async Task<IActionResult> ReportePorVendedor([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var fechaInicio = desde ?? DateTime.Today;
            var fechaFin = hasta ?? DateTime.Today;

            var reporte = await (from c in _context.ComprobanteDeVentaCabeceras
                                 join u in _context.Usuarios on c.IdVendedor equals u.IdUsuario into gj
                                 from subu in gj.DefaultIfEmpty()
                                 join p in _context.Personals on subu.IdPersonal equals p.IdPersonal into pj
                                 from subp in pj.DefaultIfEmpty()

                                 where c.Fecha.Date >= fechaInicio.Date && c.Fecha.Date <= fechaFin.Date
                                       && c.Estado == "E"

                                 group c by new { subp.Nombres, subp.Apellidos } into g
                                 select new ReporteVentasVendedorDTO
                                 {
                                     NombreVendedor = (g.Key.Nombres + " " + g.Key.Apellidos).Trim(),
                                     TotalVentas = g.Count(),
                                     TotalClientes = g.Select(c => c.Dniruc).Distinct().Count(),
                                     MontoTotal = g.Sum(c => c.Total)
                                 }).ToListAsync();

            return Ok(reporte);
        }

        [HttpGet("reporte-articulo")]
        public async Task<IActionResult> ObtenerReportePorArticulo([FromQuery] string idArticulo, [FromQuery] int mes, [FromQuery] int anio)
        {
            var articulo = await _context.Articulos.FindAsync(idArticulo);

            if (articulo == null)
                return NotFound("Artículo no encontrado.");

            var detallePorAlmacen = await _context.Almacens
                .Select(a => new DetalleArticuloPorAlmacenDTO
                {
                    NombreAlmacen = a.Nombre,
                    Ingreso = _context.MovimientosDetalles
                                .Where(m => m.IdArticulo == idArticulo && m.IdMovimientoNavigation.Tipo == "INGRESO" && m.IdMovimientoNavigation.IdAlmacen == a.IdAlmacen && m.IdMovimientoNavigation.Fecha!.Value.Month == mes && m.IdMovimientoNavigation.Fecha!.Value.Year == anio)
                                .Sum(m => (int?)m.Cantidad) ?? 0,
                    Venta = _context.ComprobanteDeVentaDetalles
                                .Where(d => d.IdArticulo == idArticulo && d.IdComprobanteNavigation.IdAlmacen == a.IdAlmacen && d.IdComprobanteNavigation.Fecha.Month == mes && d.IdComprobanteNavigation.Fecha.Year == anio)
                                .Sum(d => (int?)d.Cantidad) ?? 0,
                    Stock = _context.StockAlmacens
                                .Where(s => s.IdArticulo == idArticulo && s.IdAlmacen == a.IdAlmacen)
                                .Select(s => (int?)s.Stock)!.FirstOrDefault() ?? 0
                }).ToListAsync();

            // Calcular porcentaje
            decimal totalVentas = detallePorAlmacen.Sum(d => d.Venta);
            foreach (var d in detallePorAlmacen)
            {
                d.PorcentajeVenta = totalVentas == 0 ? 0 : (d.Venta / totalVentas) * 100;
            }

            var totalVentasMensual = await _context.ComprobanteDeVentaCabeceras
                .Where(c => c.Fecha.Month == mes && c.Fecha.Year == anio)
                .GroupBy(c => c.IdAlmacen)
                .Select(g => new ResumenVentasMensualDTO
                {
                    NombreAlmacen = _context.Almacens.Where(a => a.IdAlmacen == g.Key).Select(a => a.Nombre).FirstOrDefault(),
                    MontoTotal = g.Sum(x => x.Total)
                }).ToListAsync();

            var reporte = new ReporteArticuloDTO
            {
                IdArticulo = int.Parse(idArticulo),
                Descripcion = articulo.Descripcion,
                DetallePorAlmacen = detallePorAlmacen,
                TotalVentasMensual = totalVentasMensual
            };

            return Ok(reporte);
        }

        private async Task<int> ObtenerNuevoNumeroSerieAsync(string serie)
        {
            var ultimoNumero = await _context.ComprobanteDeVentaCabeceras
                .Where(c => c.Serie == serie)
                .OrderByDescending(c => c.Numero)
                .Select(c => c.Numero)
                .FirstOrDefaultAsync();

            return ultimoNumero + 1;
        }

        [HttpGet("reporte-ranking-vendedoras")]
        public async Task<IActionResult> ReporteRankingVendedoras([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var inicio = desde ?? DateTime.Today;
            var fin = hasta ?? DateTime.Today;

            var ranking = await (from c in _context.ComprobanteDeVentaCabeceras
                                 join u in _context.Usuarios on c.IdVendedor equals u.IdUsuario
                                 join p in _context.Personals on u.IdPersonal equals p.IdPersonal
                                 where c.Fecha.Date >= inicio.Date && c.Fecha.Date <= fin.Date
                                       && c.Estado == "E" && p.Sexo == "F"
                                 group c by new { p.Nombres, p.Apellidos } into g
                                 orderby g.Sum(c => c.Total) descending
                                 select new RankingVendedoraDTO
                                 {
                                     Vendedora = (g.Key.Nombres + " " + g.Key.Apellidos).Trim(),
                                     TotalVentas = g.Sum(c => c.Total),
                                     TotalClientes = g.Select(c => c.Dniruc).Distinct().Count(),
                                     VentasRealizadas = g.Count()
                                 }).ToListAsync();

            return Ok(ranking);
        }

        [HttpGet("reporte-top10-articulos")]
        public async Task<IActionResult> ReporteTop10Articulos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var inicio = desde ?? DateTime.Today;
            var fin = hasta ?? DateTime.Today;

            var topArticulos = await (from d in _context.ComprobanteDeVentaDetalles
                                      join c in _context.ComprobanteDeVentaCabeceras on d.IdComprobante equals c.IdComprobante
                                      where c.Fecha.Date >= inicio.Date && c.Fecha.Date <= fin.Date && c.Estado == "E"
                                      group d by new { d.IdArticulo, d.Descripcion } into g
                                      orderby g.Sum(x => x.Cantidad) descending
                                      select new TopArticuloDTO
                                      {
                                          Codigo = g.Key.IdArticulo,
                                          Descripcion = g.Key.Descripcion,
                                          TotalUnidadesVendidas = g.Sum(x => x.Cantidad),
                                          TotalImporte = g.Sum(x => x.Total)
                                      }).Take(10).ToListAsync();

            return Ok(topArticulos);
        }


        [HttpGet("emisor")]
        public async Task<IActionResult> ObtenerDatosEmisor()
        {
            // Obtener el ID de usuario desde el token (si estás usando Claims), o ajusta como necesites
            var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);

            var usuario = await _context.Usuarios
                .Include(u => u.IdPersonalNavigation)
                .ThenInclude(p => p.IdAlmacenNavigation)
                .FirstOrDefaultAsync(u => u.IdUsuario == userId);

            if (usuario == null || usuario.IdPersonalNavigation?.IdAlmacenNavigation == null)
                return NotFound("No se encontró el almacén asociado al usuario.");

            var almacen = usuario.IdPersonalNavigation.IdAlmacenNavigation;

            var emisor = new
            {
                Ruc = almacen.Ruc,
                RazonSocial = almacen.RazonSocial,
                Direccion = almacen.Direccion,
                Ubigeo = almacen.Ubigeo
            };

            return Ok(emisor);
        }

        [HttpGet("pendientes-sunat")]
        public async Task<IActionResult> ObtenerPendientesSunat([FromQuery] DateTime fecha, [FromQuery] int idAlmacen)
        {
            var pendientes = await _context.VComprobantes
                .Where(c => c.Tienda == _context.Almacens.First(a => a.IdAlmacen == idAlmacen).Nombre &&
                            c.Fecha == fecha.ToString("dd/MM/yyyy") &&
                            (c.EnviadoSunat == false || c.EnviadoSunat == null))
                .ToListAsync();

            return Ok(pendientes);
        }

        [HttpGet("dias-pendientes-envio")]
        public async Task<IActionResult> ObtenerDiasPendientesEnvio([FromQuery] int anio, [FromQuery] int idAlmacen, [FromQuery] int tipoDoc)
        {
            var nombreAlmacen = await _context.Almacens
                .Where(a => a.IdAlmacen == idAlmacen)
                .Select(a => a.Nombre)
                .FirstOrDefaultAsync();

            var tiposBuscados = tipoDoc == 1
                ? new[] { "FACTURA", "FACTURA M" }
                : new[] { "BOLETA", "BOLETA M" };

            var dias = await _context.VComprobantes
                .Where(c => c.Tienda == nombreAlmacen &&
                            c.Fecha!.EndsWith($"/{anio}") &&
                            tiposBuscados.Any(t => c.TipoDoc.ToUpper().Contains(t)) &&
                            (c.EnviadoSunat == false || c.EnviadoSunat == null))
                .Select(c => c.Fecha)
                .Distinct()
                .ToListAsync();

            // Convertir a DateTime
            var fechas = dias
                .Select(d => DateTime.TryParse(d, out var f) ? f : (DateTime?)null)
                .Where(f => f != null)
                .Select(f => f!.Value)
                .ToList();

            return Ok(fechas);
        }
    }
}