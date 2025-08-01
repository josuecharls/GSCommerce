using GSCommerce.Client.Models;
using GSCommerce.Client.Models.DTOs.Reportes;
using GSCommerce.Client.Models.SUNAT;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using GSCommerceAPI.Models.SUNAT.DTOs;
using GSCommerceAPI.Services.SUNAT;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GSCommerce.Client.Models; // For VentaDiariaAlmacenDTO


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

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerVentaPorId(int id)
        {
            var cabecera = await _context.ComprobanteDeVentaCabeceras
                .Include(c => c.ComprobanteDeVentaDetalles)
                .FirstOrDefaultAsync(c => c.IdComprobante == id);

            if (cabecera == null)
                return NotFound();

            var venta = new VentaDTO
            {
                IdTipoDocumento = cabecera.IdTipoDocumento,
                Serie = cabecera.Serie,
                Numero = cabecera.Numero,
                Fecha = cabecera.Fecha,
                IdCliente = cabecera.IdCliente,
                Dniruc = cabecera.Dniruc,
                Nombre = cabecera.Nombre,
                Direccion = cabecera.Direccion,
                TipoCambio = cabecera.TipoCambio,
                SubTotal = cabecera.SubTotal,
                Igv = cabecera.Igv,
                Total = cabecera.Total,
                Redondeo = cabecera.Redondeo,
                Apagar = cabecera.Apagar,
                AFavor = cabecera.Total,
                IdVendedor = cabecera.IdVendedor,
                IdCajero = cabecera.IdCajero,
                IdAlmacen = cabecera.IdAlmacen,
                Detalles = cabecera.ComprobanteDeVentaDetalles.Select(d => new VentaDetalleDTO
                {
                    Item = d.Item,
                    CodigoItem = d.IdArticulo,
                    DescripcionItem = d.Descripcion,
                    UnidadMedida = d.UnidadMedida,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.Precio,
                    PorcentajeDescuento = d.PorcentajeDescuento,
                    Total = d.Total
                }).ToList()
            };

            return Ok(venta);
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

        [HttpGet("resumen")]
        public async Task<IActionResult> ObtenerResumen([FromQuery] int idAlmacen, [FromQuery] int idUsuario)
        {
            var fechaHoy = DateOnly.FromDateTime(DateTime.Today);

            var resumen = new ResumenDiarioDTO();

            var ventas = await _context.VRecaudacion3s
                .Where(v => v.Fecha == fechaHoy && v.IdAlmacen == idAlmacen && v.IdCajero == idUsuario)
                .ToListAsync();

            foreach (var v in ventas)
            {
                switch (v.Descripcion)
                {
                    case "Efectivo": resumen.Efectivo = v.Monto ?? 0; break;
                    case "Tarjeta": resumen.Tarjeta = v.Monto ?? 0; break;
                    case "N.C.": resumen.NotaCredito = v.Monto ?? 0; break;
                }
            }

            var cierres = await _context.VCierreEnLinea1s
                .Where(c => c.Fecha == fechaHoy && c.IdAlmacen == idAlmacen && c.IdUsuario == idUsuario)
                .ToListAsync();

            foreach (var c in cierres)
            {
                switch (c.Categoria)
                {
                    case "Saldo Inicial": resumen.SaldoInicial = c.Monto ?? 0; break;
                    case "I": resumen.Ingresos = c.Monto ?? 0; break;
                    case "E": resumen.Egresos = c.Monto ?? 0; break;
                }
            }

            return Ok(resumen);
        }

        [HttpGet("ventas-almacenes-dia")]
        public async Task<IActionResult> ObtenerVentasAlmacenesDia([FromQuery] string fecha)
        {
            if (!DateOnly.TryParse(fecha, out var fechaDia))
                return BadRequest("Fecha inválida");

            var inicio = fechaDia.ToDateTime(new TimeOnly(0, 0, 0));
            var fin = fechaDia.ToDateTime(new TimeOnly(23, 59, 59));

            var ventas = await _context.ComprobanteDeVentaCabeceras
                .Where(c => c.Fecha >= inicio && c.Fecha <= fin && c.Estado == "E")
                .GroupBy(c => c.IdAlmacen)
                .Select(g => new VentaDiariaAlmacenDTO
                {
                    IdAlmacen = g.Key,
                    NombreAlmacen = _context.Almacens.Where(a => a.IdAlmacen == g.Key).Select(a => a.Nombre).FirstOrDefault() ?? "",
                    Total = g.Sum(c => c.Total)
                })
                .ToListAsync();

            return Ok(ventas);
        }


        [HttpPost]
        public async Task<IActionResult> RegistrarVenta([FromBody] VentaRegistroDTO ventaRegistro)
        {
            if (ventaRegistro?.Cabecera == null || ventaRegistro.TipoDocumento == null || ventaRegistro.TipoDocumento.IdTipoDocumentoVenta == 0)
                return BadRequest("Falta seleccionar un tipo de documento válido.");

            // Verificar que exista una apertura de caja para el día actual en el almacén indicado
            var fechaActual = DateOnly.FromDateTime(DateTime.Today);
            var idAlmacen = ventaRegistro.Cabecera.IdAlmacen;
            var idCajero = ventaRegistro.Cabecera.IdCajero;
            Console.WriteLine($"Verificando apertura: almacen={idAlmacen}, cajero={idCajero}, fecha={fechaActual}");
            var cajaAbierta = await _context.AperturaCierreCajas
                .AnyAsync(a => a.IdAlmacen == idAlmacen && a.IdUsuario == idCajero && a.Fecha == fechaActual && a.Estado == "A");

            if (!cajaAbierta)
                return BadRequest("No se puede registrar la venta porque la caja no está aperturada.");

            // Verificar stock disponible por artículo
            foreach (var d in ventaRegistro.Detalles)
            {
                var stock = await _context.StockAlmacens
                    .FirstOrDefaultAsync(s => s.IdAlmacen == idAlmacen && s.IdArticulo == d.CodigoItem);

                if (stock == null || stock.Stock < d.Cantidad)
                {
                    return BadRequest($"Stock insuficiente para el artículo {d.DescripcionItem}");
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            var nuevoNumero = await ObtenerNuevoNumeroSerieAsync(ventaRegistro.Cabecera.Serie);

            try
            {
                Cliente? cliente = null;
                var documentoCliente = ventaRegistro.Cabecera.DocumentoCliente;
                if (!string.IsNullOrWhiteSpace(documentoCliente))
                {
                    cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Dniruc == documentoCliente);
                    if (cliente == null)
                    {
                        cliente = new Cliente
                        {
                            TipoDocumento = documentoCliente.Length == 11 ? "RUC" : "DNI",
                            Dniruc = documentoCliente,
                            Nombre = ventaRegistro.Cabecera.NombreCliente,
                            Direccion = ventaRegistro.Cabecera.DireccionCliente,
                            Estado = true
                        };
                        _context.Clientes.Add(cliente);
                        await _context.SaveChangesAsync();
                    }
                }

                var monedaKey = $"MonedaAlmacen_{idAlmacen}";
                var monedaConfig = await _context.Configuracions.FirstOrDefaultAsync(c => c.Configuracion1 == monedaKey);
                var monedaAlmacen = monedaConfig?.Valor ?? "PEN";

                var tipoCambio = 1m;
                if (monedaAlmacen == "USD")
                {
                    var hoy = DateOnly.FromDateTime(DateTime.Today);
                    var tc = await _context.TipoDeCambios.FirstOrDefaultAsync(t => t.Fecha == hoy);
                    tipoCambio = tc?.Venta ?? 1m;
                }

                // Insertar cabecera
                var cabecera = new ComprobanteDeVentaCabecera
                {
                    IdTipoDocumento = ventaRegistro.TipoDocumento.IdTipoDocumentoVenta,
                    Serie = ventaRegistro.Cabecera.Serie,
                    Numero = nuevoNumero,
                    Fecha = DateTime.Now,
                    IdCliente = cliente?.IdCliente,
                    Nombre = ventaRegistro.Cabecera.NombreCliente,
                    Dniruc = ventaRegistro.Cabecera.DocumentoCliente,
                    Direccion = ventaRegistro.Cabecera.DireccionCliente,
                    TipoCambio = tipoCambio,
                    SubTotal = ventaRegistro.Cabecera.SubTotal,
                    Igv = ventaRegistro.Cabecera.Igv,
                    Total = ventaRegistro.Cabecera.Total,
                    Redondeo = 0,
                    Apagar = ventaRegistro.Cabecera.APagar,
                    IdVendedor = ventaRegistro.Cabecera.IdVendedor,
                    IdCajero = ventaRegistro.Cabecera.IdCajero ?? 0,
                    IdAlmacen = idAlmacen,
                    Estado = "E",
                    FechaHoraRegistro = DateTime.Now,
                };

                _context.ComprobanteDeVentaCabeceras.Add(cabecera);
                await _context.SaveChangesAsync();  // Para generar el ID de la cabecera

                // Insertar detalles y actualizar stock
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

                    var stock = await _context.StockAlmacens
                        .FirstOrDefaultAsync(s => s.IdAlmacen == idAlmacen && s.IdArticulo == detalle.CodigoItem);
                    if (stock != null)
                    {
                        stock.Stock -= (int)detalle.Cantidad;
                    }
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

            var query = from c in _context.ComprobanteDeVentaCabeceras
                        join p in _context.Personals on c.IdVendedor equals p.IdPersonal into gj
                        from p in gj.DefaultIfEmpty()
                        where c.Fecha.Date >= fechaInicio.Date && c.Fecha.Date <= fechaFin.Date
                              && c.Estado == "E"
                        select new { c, p };

            var reporte = await query
                .GroupBy(x => new { x.p.Nombres, x.p.Apellidos })
                .Select(g => new ReporteVentasVendedorDTO
                {
                    NombreVendedor = ((g.Key.Nombres ?? "") + " " + (g.Key.Apellidos ?? "")).Trim(),
                    TotalVentas = g.Count(),
                    TotalClientes = g.Select(x => x.c.Dniruc).Distinct().Count(),
                    MontoTotal = g.Sum(x => x.c.Total)
                })
                .ToListAsync();

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
        public async Task<IActionResult> ReporteRankingVendedoras([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? idAlmacen, [FromQuery] bool porAlmacen = false)
        {
            var inicio = desde ?? DateTime.Today;
            var fin = hasta ?? DateTime.Today;

            var query = from c in _context.ComprobanteDeVentaCabeceras
                        join p in _context.Personals on c.IdVendedor equals p.IdPersonal
                        where c.Fecha.Date >= inicio.Date && c.Fecha.Date <= fin.Date
                              && c.Estado == "E"
                        select new { c, p };

            if (porAlmacen && idAlmacen.HasValue)
            {
                query = query.Where(x => x.c.IdAlmacen == idAlmacen.Value);
            }

            var ranking = await query
                .GroupBy(x => new { x.p.Nombres, x.p.Apellidos })
                .OrderByDescending(g => g.Sum(x => x.c.Total))
                .Select(g => new RankingVendedoraDTO
                {
                    Vendedora = (g.Key.Nombres + " " + g.Key.Apellidos).Trim(),
                    TotalVentas = g.Sum(x => x.c.Total),
                    TotalClientes = g.Select(x => x.c.Dniruc).Distinct().Count(),
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

        [Authorize]
        [HttpGet("emisor")]
        public async Task<IActionResult> ObtenerDatosEmisor()
        {
            // Obtener el ID de usuario desde el token (si estás usando Claims), o ajusta como necesites
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("El token no contiene el ID de usuario.");

            if (!int.TryParse(userIdClaim, out var userId))
                return BadRequest("ID de usuario inválido.");

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

        [HttpPost("generar-resumen")]
        public async Task<IActionResult> GenerarResumenDiario([FromQuery] DateTime fecha, [FromQuery] int idAlmacen)
        {
            var datosAlmacen = await _context.Almacens
                .Where(a => a.IdAlmacen == idAlmacen)
                .Select(a => new { a.Nombre, a.Ruc, a.RazonSocial, a.Direccion, a.Ubigeo })
                .FirstOrDefaultAsync();

            if (datosAlmacen == null)
                return BadRequest("Almacén no encontrado");

            var pendientes = await _context.VComprobantes
                .Where(c => c.Tienda == datosAlmacen.Nombre &&
                            c.Fecha == fecha.ToString("dd/MM/yyyy") &&
                            (c.EnviadoSunat == false || c.EnviadoSunat == null))
                .ToListAsync();

            if (!pendientes.Any())
                return BadRequest("No hay comprobantes pendientes");

            var lista = new List<GSCommerceAPI.Models.SUNAT.DTOs.ComprobanteCabeceraDTO>();

            foreach (var p in pendientes)
            {
                var cab = await _context.ComprobanteDeVentaCabeceras
                    .Include(c => c.ComprobanteDeVentaDetalles)
                    .Include(c => c.IdTipoDocumentoNavigation)
                    .FirstOrDefaultAsync(c => c.IdComprobante == p.IdComprobante);

                if (cab == null) continue;

                var dto = new GSCommerceAPI.Models.SUNAT.DTOs.ComprobanteCabeceraDTO
                {
                    IdComprobante = cab.IdComprobante,
                    TipoDocumento = cab.IdTipoDocumentoNavigation.Abreviatura,
                    Serie = cab.Serie,
                    Numero = cab.Numero,
                    FechaEmision = cab.Fecha,
                    HoraEmision = cab.Fecha.TimeOfDay,
                    RucEmisor = datosAlmacen.Ruc ?? string.Empty,
                    RazonSocialEmisor = datosAlmacen.RazonSocial ?? string.Empty,
                    DireccionEmisor = datosAlmacen.Direccion ?? string.Empty,
                    UbigeoEmisor = datosAlmacen.Ubigeo ?? string.Empty,
                    DocumentoCliente = cab.Dniruc ?? string.Empty,
                    TipoDocumentoCliente = (cab.Dniruc != null && cab.Dniruc.Length == 11) ? "6" : "1",
                    NombreCliente = cab.Nombre,
                    DireccionCliente = cab.Direccion ?? string.Empty,
                    SubTotal = cab.SubTotal,
                    Igv = cab.Igv,
                    Total = cab.Total,
                    Detalles = cab.ComprobanteDeVentaDetalles.Select(d => new GSCommerceAPI.Models.SUNAT.DTOs.ComprobanteDetalleDTO
                    {
                        Item = d.Item,
                        CodigoItem = d.IdArticulo,
                        DescripcionItem = d.Descripcion,
                        UnidadMedida = d.UnidadMedida,
                        Cantidad = d.Cantidad,
                        PrecioUnitarioConIGV = d.Precio,
                        PrecioUnitarioSinIGV = Math.Round(d.Precio / 1.18m, 2),
                        IGV = Math.Round((d.Precio * d.Cantidad) - (d.Precio * d.Cantidad / 1.18m), 2)
                    }).ToList()
                };

                lista.Add(dto);
            }

            var resultado = await _facturacionService.EnviarResumenDiario(lista);

            if (resultado.exito)
                return Ok(resultado.mensaje);

            return BadRequest(resultado.mensaje);
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