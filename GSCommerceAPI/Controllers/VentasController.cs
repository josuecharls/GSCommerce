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
using GSCommerceAPI.Models.Reportes; // For VentaDiariaAlmacenDTO


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

        [HttpGet("reporte-total-tiendas")]
        public async Task<ActionResult<List<ReporteTotalTiendasDTO>>> GetReporteTotalTiendas(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta,
            [FromQuery] int? idAlmacen // ← filtro opcional
        )
        {
            var hastaExcl = hasta.Date.AddDays(1);

            var query =
                from c in _context.ComprobanteDeVentaCabeceras
                where c.Fecha >= desde.Date && c.Fecha < hastaExcl
                      && (c.Estado != "ANULADO") // ajusta a tu esquema real
                      && (!idAlmacen.HasValue || c.IdAlmacen == idAlmacen.Value) // ← aplica filtro si viene
                group c by c.IdAlmacen into g
                select new
                {
                    IdAlmacen = g.Key,
                    Venta = g.Sum(x => x.Total)
                };

            var montos = await query.ToListAsync();
            var ids = montos.Select(x => x.IdAlmacen).ToList();

            var nombres = await _context.Almacens
                .Where(a => ids.Contains(a.IdAlmacen))
                .Select(a => new { a.IdAlmacen, a.Nombre })
                .ToListAsync();

            var totalGlobal = montos.Sum(x => x.Venta);

            var resultado = montos
                .Select(x =>
                {
                    var nombre = nombres.FirstOrDefault(n => n.IdAlmacen == x.IdAlmacen)?.Nombre ?? $"Almacén {x.IdAlmacen}";
                    var porcentaje = totalGlobal > 0 ? Math.Round((double)(x.Venta / totalGlobal) * 100, 2) : 0;
                    return new ReporteTotalTiendasDTO
                    {
                        IdAlmacen = x.IdAlmacen,
                        Tienda = nombre,
                        Venta = x.Venta,
                        Porcentaje = porcentaje
                    };
                })
                .OrderBy(r => r.Venta) // o .OrderByDescending(r => r.Venta)
                .ToList();

            return resultado;
        }


        [HttpPost("reporte-articulos-rango")]
        public async Task<IActionResult> ObtenerReporteArticulosRango([FromBody] ReporteArticulosRangoRequest req)
        {
            if (req.Ids == null || req.Ids.Count == 0)
                return BadRequest("Debe enviar al menos un código de artículo.");
            var desde = req.Desde.Date;
            var hasta = req.Hasta.Date;

            // Meses del rango
            var meses = new List<Models.Reportes.MesColDTO>();
            var cursor = new DateTime(desde.Year, desde.Month, 1);
            var fin = new DateTime(hasta.Year, hasta.Month, 1);
            while (cursor <= fin)
            {
                meses.Add(new Models.Reportes.MesColDTO
                {
                    Year = cursor.Year,
                    Month = cursor.Month,
                    Label = System.Globalization.CultureInfo
                                .GetCultureInfo("es-PE")
                                .DateTimeFormat
                                .GetAbbreviatedMonthName(cursor.Month)
                                .ToUpperInvariant().Replace(".", "")
                });
                cursor = cursor.AddMonths(1);
            }

            // Catálogos
            var almacenesDic = await _context.Almacens
                .AsNoTracking()
                .ToDictionaryAsync(a => a.IdAlmacen, a => a.Nombre);

            var arts = await _context.Articulos
                .AsNoTracking()
                .Where(a => req.Ids.Contains(a.IdArticulo))
                .Select(a => new { a.IdArticulo, a.Descripcion, a.PrecioCompra, a.PrecioVenta })
                .ToListAsync();

            // Ventas por mes x almacén
            var ventas = await _context.ComprobanteDeVentaDetalles
                .AsNoTracking()
                .Where(d => req.Ids.Contains(d.IdArticulo)
                         && d.IdComprobanteNavigation.Fecha >= desde
                         && d.IdComprobanteNavigation.Fecha < hasta.AddDays(1))
                .Select(d => new {
                    d.IdArticulo,
                    d.IdComprobanteNavigation.IdAlmacen,
                    Mes = new DateTime(d.IdComprobanteNavigation.Fecha.Year, d.IdComprobanteNavigation.Fecha.Month, 1),
                    Cant = d.Cantidad
                })
                .GroupBy(x => new { x.IdArticulo, x.IdAlmacen, x.Mes })
                .Select(g => new {
                    g.Key.IdArticulo,
                    g.Key.IdAlmacen,
                    g.Key.Mes,
                    Cant = g.Sum(x => x.Cant)
                })
                .ToListAsync();

            // Ingresos (solo movimientos tipo INGRESO)
            var ingresos = await _context.MovimientosDetalles
                .AsNoTracking()
                .Where(m => req.Ids.Contains(m.IdArticulo)
                         && m.IdMovimientoNavigation.Tipo == "I"
                         && m.IdMovimientoNavigation.Fecha.HasValue
                         && m.IdMovimientoNavigation.Fecha.Value >= DateOnly.FromDateTime(desde)
                         && m.IdMovimientoNavigation.Fecha.Value < DateOnly.FromDateTime(hasta.AddDays(1)))
                .Select(m => new {
                    m.IdArticulo,
                    m.IdMovimientoNavigation.IdAlmacen,
                    m.Cantidad,
                    m.IdMovimientoNavigation.Fecha
                })
                .ToListAsync();

            var ingresosAgg = ingresos
                .GroupBy(x => new { x.IdArticulo, x.IdAlmacen })
                .Select(g => new {
                    g.Key.IdArticulo,
                    g.Key.IdAlmacen,
                    Cant = g.Sum(x => x.Cantidad),
                    Fch = g.Min(x => x.Fecha)
                })
                .ToList();

            // Stock actual
            var stock = await _context.StockAlmacens
                .AsNoTracking()
                .Where(s => req.Ids.Contains(s.IdArticulo))
                .Select(s => new { s.IdArticulo, s.IdAlmacen, s.Stock })
                .ToListAsync();

            var respuesta = new List<Models.Reportes.ReporteArticuloRangoDTO>();

            foreach (var art in arts)
            {
                // almacenes involucrados (por ventas, ingresos o stock)
                var almacenesArt = ventas.Where(v => v.IdArticulo == art.IdArticulo).Select(v => v.IdAlmacen)
                    .Concat(ingresosAgg.Where(i => i.IdArticulo == art.IdArticulo).Select(i => i.IdAlmacen))
                    .Concat(stock.Where(s => s.IdArticulo == art.IdArticulo).Select(s => s.IdAlmacen))
                    .Distinct()
                    .ToList();

                var filas = new List<Models.Reportes.FilaAlmacenDTO>();

                foreach (var idAlm in almacenesArt)
                {
                    var nombre = almacenesDic.TryGetValue(idAlm, out var nm) ? nm : idAlm.ToString();

                    var ing = ingresosAgg.FirstOrDefault(i => i.IdArticulo == art.IdArticulo && i.IdAlmacen == idAlm);
                    var st = stock.FirstOrDefault(s => s.IdArticulo == art.IdArticulo && s.IdAlmacen == idAlm);

                    var ventasAlm = ventas
                        .Where(v => v.IdArticulo == art.IdArticulo && v.IdAlmacen == idAlm)
                        .ToDictionary(v => $"{v.Mes:yyyyMM}", v => v.Cant);

                    var fila = new Models.Reportes.FilaAlmacenDTO
                    {
                        NombreAlmacen = nombre,
                        Codigo = art.IdArticulo,
                        Ingreso = ing?.Cant ?? 0,
                        FechaPrimerIngreso = ing?.Fch != null ? ing.Fch.Value.ToDateTime(new TimeOnly(0, 0)) : null,
                        Stock = st?.Stock ?? 0,
                        PC = art.PrecioCompra,
                        PV = art.PrecioVenta
                    };

                    // completar todos los meses
                    foreach (var m in meses)
                    {
                        var key = $"{m.Year:D4}{m.Month:D2}";
                        fila.VentasPorMes[key] = ventasAlm.TryGetValue(key, out var cant) ? cant : 0;
                    }

                    fila.TotalVentas = fila.VentasPorMes.Values.Sum();
                    fila.PorcentajeVendida = fila.Ingreso > 0 ? Math.Round((decimal)fila.TotalVentas / fila.Ingreso * 100m, 2) : 0m;

                    filas.Add(fila);
                }

                // Totales
                var tot = new Models.Reportes.TotalesFilaDTO
                {
                    Ingreso = filas.Sum(f => f.Ingreso),
                    Stock = filas.Sum(f => f.Stock)
                };
                foreach (var m in meses)
                {
                    var key = $"{m.Year:D4}{m.Month:D2}";
                    tot.VentasPorMes[key] = filas.Sum(f => f.VentasPorMes[key]);
                }
                tot.TotalVentas = tot.VentasPorMes.Values.Sum();
                tot.PorcentajeVendida = tot.Ingreso > 0 ? Math.Round((decimal)tot.TotalVentas / tot.Ingreso * 100m, 2) : 0m;

                respuesta.Add(new Models.Reportes.ReporteArticuloRangoDTO
                {
                    IdArticulo = art.IdArticulo,
                    Descripcion = art.Descripcion,
                    PrecioCompra = art.PrecioCompra,
                    PrecioVenta = art.PrecioVenta,
                    Meses = meses,
                    Filas = filas.OrderBy(f => f.NombreAlmacen).ToList(),
                    Totales = tot
                });
            }

            return Ok(respuesta);
        }

        [HttpGet("list")]
        [Authorize]
        public async Task<IActionResult> ListarVentas(
            [FromQuery] int? idAlmacen,
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            // acepta ambos nombres desde el cliente: tipoDoc e idTipoDoc
            [FromQuery(Name = "tipoDoc")] int? tipoDoc,
            [FromQuery(Name = "idTipoDoc")] int? idTipoDocAlias,
            // opcional: buscar por documento del cliente
            [FromQuery] string? dniruc
        )
        {
            var inicio = (desde?.Date) ?? DateTime.Today;
            var finExcl = ((hasta?.Date) ?? inicio).AddDays(1);

            var query = _context.VVenta1s.Where(v => v.Fecha >= inicio && v.Fecha < finExcl);

            // Filtro por almacén según cargo
            var cargo = User.FindFirst("Cargo")?.Value ?? string.Empty;
            if (!string.Equals(cargo, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase))
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (int.TryParse(userIdClaim, out var userId))
                {
                    var userAlmacen = await _context.Usuarios
                        .Where(u => u.IdUsuario == userId)
                        .Select(u => u.IdPersonalNavigation != null ? (int?)u.IdPersonalNavigation.IdAlmacen : null)
                        .FirstOrDefaultAsync();

                    if (!userAlmacen.HasValue) return Ok(new List<VentaConsultaDTO>());
                    query = query.Where(v => v.IdAlmacen == userAlmacen.Value);
                }
                else
                {
                    return Ok(new List<VentaConsultaDTO>());
                }
            }
            else if (idAlmacen.HasValue && idAlmacen.Value > 0)
            {
                query = query.Where(v => v.IdAlmacen == idAlmacen.Value);
            }

            // Filtro por tipo de documento
            var tipo = tipoDoc ?? idTipoDocAlias;
            if (tipo.HasValue)
                query = query.Where(v => v.IdTipoDocumento == tipo.Value);

            // Filtro por documento del cliente (DNI/RUC)
            if (!string.IsNullOrWhiteSpace(dniruc))
                query = query.Where(v => v.Dniruc != null && v.Dniruc.Contains(dniruc));

            var ventas = await query
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

            // Agrupar formas de pago
            var ids = ventas.Select(v => v.IdComprobante).ToList();
            if (ids.Count > 0)
            {
                var pagosList = await _context.VDetallePagoVenta1s
                    .Where(p => ids.Contains(p.IdComprobante))
                    .Select(p => new { p.IdComprobante, p.Descripcion })
                    .ToListAsync();

                var pagos = pagosList
                    .GroupBy(p => p.IdComprobante)
                    .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(p => p.Descripcion)));

                foreach (var v in ventas)
                    if (pagos.TryGetValue(v.IdComprobante, out var forma))
                        v.FormaPago = forma;
            }

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
                var descripcion = (v.Descripcion ?? string.Empty)
                    .Split(' ')[0]
                    .ToLowerInvariant();

                switch (descripcion)
                {
                    case "efectivo":
                        resumen.Efectivo += v.Monto ?? 0;
                        break;
                    case "tarjeta":
                    case "online":
                        resumen.Tarjeta += v.Monto ?? 0;
                        break;
                    case "n.c":
                    case "n.c.":
                        resumen.NotaCredito += v.Monto ?? 0;
                        break;
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

            var nuevoNumero = await ObtenerNuevoNumeroSerieAsync(
                ventaRegistro.Cabecera.Serie,
                idAlmacen,
                ventaRegistro.TipoDocumento.IdTipoDocumentoVenta);

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

                // Registrar como pendiente de envío a SUNAT
                var compFe = new Comprobante
                {
                    IdComprobante = cabecera.IdComprobante,
                    Hash = string.Empty,
                    EnviadoSunat = false,
                    Estado = false,
                    EsNota = false
                };
                _context.Comprobantes.Add(compFe);

                await _context.SaveChangesAsync(); // Guardar detalles, pagos y estado SUNAT
                await transaction.CommitAsync();

                return Ok(new VentaResponseDTO
                {
                    Numero = cabecera.Numero,
                    IdComprobante = cabecera.IdComprobante,
                    PendienteSunat = true
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
            var ini = desde.Date;
            var finExclusivo = hasta.Date.AddDays(1);

            var lista = await (
                from c in _context.ComprobanteDeVentaCabeceras
                join td in _context.TipoDocumentoVenta   // DbSet real
                    on c.IdTipoDocumento equals td.IdTipoDocumentoVenta
                join s in _context.Comprobantes
                    on c.IdComprobante equals s.IdComprobante into gj
                from sub in gj.DefaultIfEmpty()
                where c.Fecha >= ini && c.Fecha < finExclusivo
                      && td.IdTipoDocumentoVenta != 4      // excluir TICKET
                select new EstadoSunatDTO
                {
                    IdComprobante = c.IdComprobante,
                    TipoDocumento = td.Descripcion,         // "BOLETA", "FACTURA", "BOLETA M", "FACTURA M"
                    Serie = c.Serie,
                    Numero = c.Numero,
                    FechaEmision = c.Fecha,
                    EstadoSunat = (sub == null || sub.EnviadoSunat != true)
                        ? "PENDIENTE"
                        : (sub.Estado ? "ACEPTADO" : "RECHAZADO"),
                    DescripcionSunat = sub != null ? sub.RespuestaSunat : null
                }
            ).ToListAsync();

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

            var totalVentasMensual = await _context.ComprobanteDeVentaDetalles
                .Where(d => d.IdArticulo == idArticulo &&
                            d.IdComprobanteNavigation.Fecha.Month == mes &&
                            d.IdComprobanteNavigation.Fecha.Year == anio)
                .GroupBy(d => d.IdComprobanteNavigation.IdAlmacen)
                .Select(g => new ResumenVentasMensualDTO
                {
                    NombreAlmacen = _context.Almacens
                        .Where(a => a.IdAlmacen == g.Key)
                        .Select(a => a.Nombre)
                        .FirstOrDefault(),
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

        private async Task<int> ObtenerNuevoNumeroSerieAsync(string serie, int idAlmacen, int idTipoDocumento)
        {
            var correlativo = await _context.SerieCorrelativos
                .Where(s => s.Serie == serie && s.IdAlmacen == idAlmacen && s.IdTipoDocumentoVenta == idTipoDocumento)
                .Select(s => s.Correlativo)
                .FirstOrDefaultAsync();

            return correlativo + 1;
        }

        [Authorize]
        [HttpGet("reporte-ranking-vendedoras")]
        public async Task<IActionResult> ReporteRankingVendedoras([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? idAlmacen, [FromQuery] bool porAlmacen = false)
        {
            var cargo = User.FindFirst("Cargo")?.Value;
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (cargo == "CAJERO" && int.TryParse(userIdClaim, out var userId))
            {
                idAlmacen = await _context.Usuarios
                    .Include(u => u.IdPersonalNavigation)
                    .Where(u => u.IdUsuario == userId)
                    .Select(u => u.IdPersonalNavigation.IdAlmacen)
                    .FirstOrDefaultAsync();
                porAlmacen = true;
            }

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

        [Authorize]
        [HttpGet("reporte-top10-articulos")]
        public async Task<IActionResult> ReporteTop10Articulos(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] int? idAlmacen,
            [FromQuery] int top = 10)
        {
            var start = (desde ?? DateTime.Today).Date;
            var end = (hasta ?? DateTime.Today).Date.AddDays(1);
            top = top <= 0 ? 10 : top;

            var cargo = User.FindFirst("Cargo")?.Value;
            var idAlmClaim = User.FindFirst("IdAlmacen")?.Value;

            // Si es CAJERO, se fuerza su almacén del token.
            int? idAlmacenForzado = null;
            if (string.Equals(cargo, "CAJERO", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(idAlmClaim, out var alm))
            {
                idAlmacenForzado = alm;
            }

            var q = from d in _context.ComprobanteDeVentaDetalles
                    join c in _context.ComprobanteDeVentaCabeceras on d.IdComprobante equals c.IdComprobante
                    where c.Fecha >= start && c.Fecha < end && c.Estado == "E"
                    select new { d, c };

            if (idAlmacenForzado.HasValue)
                q = q.Where(x => x.c.IdAlmacen == idAlmacenForzado.Value);
            else if (idAlmacen.HasValue && idAlmacen.Value > 0)
                q = q.Where(x => x.c.IdAlmacen == idAlmacen.Value);

            var resultado = await q
                .GroupBy(x => new { x.d.IdArticulo, x.d.Descripcion })
                .Select(g => new TopArticuloDTO
                {
                    Codigo = g.Key.IdArticulo,
                    Descripcion = g.Key.Descripcion,
                    TotalUnidadesVendidas = g.Sum(x => x.d.Cantidad),
                    TotalImporte = g.Sum(x => x.d.Total)
                })
                .OrderByDescending(x => x.TotalImporte)
                .Take(top)
                .ToListAsync();

            return Ok(resultado);
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
        [Authorize]
        public async Task<IActionResult> ObtenerPendientesSunat([FromQuery] DateTime? fecha, [FromQuery] int? idAlmacen)
        {
            var day = (fecha ?? DateTime.Today).Date;
            var start = day;
            var end = day.AddDays(1);

            var q = from c in _context.ComprobanteDeVentaCabeceras.AsNoTracking()
                    join f in _context.Comprobantes.AsNoTracking() on c.IdComprobante equals f.IdComprobante into cf
                    from f in cf.DefaultIfEmpty()
                    join a in _context.Almacens.AsNoTracking() on c.IdAlmacen equals a.IdAlmacen
                    join t in _context.TipoDocumentoVenta.AsNoTracking() on c.IdTipoDocumento equals t.IdTipoDocumentoVenta
                    where c.Fecha >= start && c.Fecha < end
                          && c.IdTipoDocumento != 4 // excluir TICKET
                          && (f == null || f.EnviadoSunat == false || f.EnviadoSunat == null || f.Estado == false)
                    select new
                    {
                        c.IdComprobante,
                        c.IdAlmacen,
                        Tienda = a.Nombre,
                        TipoDoc = t.Descripcion,
                        c.Serie,
                        c.Numero,
                        c.Apagar,
                        c.Fecha,
                        IdFe = (int?)(f != null ? f.IdFe : 0),
                        Hash = f != null ? f.Hash : null,
                        EnviadoSunat = f != null && f.EnviadoSunat == true,
                        FechaEnvio = f != null ? f.FechaEnvio : null,
                        FechaRespuestaSunat = f != null ? f.FechaRespuestaSunat : null,
                        RespuestaSunat = f != null ? f.RespuestaSunat : null,
                        TicketSunat = f != null ? f.TicketSunat : null,
                        Xml = f != null ? f.Xml : null
                    };

            if (idAlmacen.HasValue && idAlmacen.Value > 0)
                q = q.Where(x => x.IdAlmacen == idAlmacen.Value);

            var rows = await q
                .OrderBy(x => x.Tienda).ThenBy(x => x.Serie).ThenBy(x => x.Numero)
                .ToListAsync();

            // 🚫 Nada de ToString("...")/PadLeft en LINQ-to-Entities; aquí ya es LINQ-to-Objects
            var pendientes = rows.Select(x => new PendienteSunatDTO
            {
                IdFe = x.IdFe ?? 0,
                IdAlmacen = x.IdAlmacen,
                Tienda = x.Tienda,
                TipoDoc = x.TipoDoc,
                Numero = $"{x.Serie}-{x.Numero:D8}",
                Fecha = x.Fecha.ToString("dd/MM/yyyy"),
                Apagar = x.Apagar,
                Hash = x.Hash ?? string.Empty,
                EnviadoSunat = x.EnviadoSunat,
                FechaEnvio = x.FechaEnvio,
                FechaRespuestaSunat = x.FechaRespuestaSunat,
                RespuestaSunat = x.RespuestaSunat,
                TicketSunat = x.TicketSunat,
                Xml = x.Xml,
                IdComprobante = x.IdComprobante
            }).ToList();

            return Ok(pendientes);
        }

        // GET: api/ventas/xml/123
        [HttpGet("xml/{idComprobante:int}")]
        [Authorize]
        public async Task<IActionResult> ObtenerXml(int idComprobante)
        {
            var xml = await _context.Comprobantes
                .Where(c => c.IdComprobante == idComprobante)
                .Select(c => c.Xml)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(xml))
                return NotFound("No hay XML almacenado para este comprobante.");

            return Content(xml, "application/xml");
        }

        // PUT: api/ventas/xml/123  (body: { "xml": "<Invoice .../>" })
        public class XmlUpdateDTO { public string Xml { get; set; } = string.Empty; }

        [HttpPut("xml/{idComprobante:int}")]
        [Authorize(Roles = "ADMINISTRADOR")] // si quieres restringir edición
        public async Task<IActionResult> ActualizarXml(int idComprobante, [FromBody] XmlUpdateDTO body)
        {
            if (body is null || string.IsNullOrWhiteSpace(body.Xml))
                return BadRequest("XML vacío.");

            var comp = await _context.Comprobantes.FirstOrDefaultAsync(c => c.IdComprobante == idComprobante);
            if (comp == null) return NotFound("Comprobante no existe.");

            // Guardamos el XML editado y lo dejamos como pendiente
            comp.Xml = body.Xml;
            comp.EnviadoSunat = false;
            comp.Estado = true;                  // NO borrar
            comp.RespuestaSunat = null;
            comp.TicketSunat = null;
            comp.FechaEnvio = null;
            comp.FechaRespuestaSunat = null;
            await _context.SaveChangesAsync();

            return Ok("XML actualizado. (Asegúrate que esté firmado para re-enviar)");
        }

        [HttpPost("reintentar/{idComprobante:int}")]
        [Authorize]
        public async Task<IActionResult> ReintentarDesdeXml(int idComprobante, [FromQuery] string modo = "bill")
        {
            // 1) Comprobante (XML)
            var comp = await _context.Comprobantes
                .FirstOrDefaultAsync(c => c.IdComprobante == idComprobante);
            if (comp == null) return NotFound("Comprobante no existe.");
            if (string.IsNullOrWhiteSpace(comp.Xml)) return BadRequest("No hay XML para enviar.");

            // 2) Cabecera (serie, número, tipo, almacén)
            var cab = await _context.ComprobanteDeVentaCabeceras
                .Include(c => c.IdTipoDocumentoNavigation)
                .FirstOrDefaultAsync(c => c.IdComprobante == idComprobante);
            if (cab == null) return NotFound("Cabecera no encontrada.");

            // 3) Almacén (RUC, SOL)
            var alm = await _context.Almacens
                .Where(a => a.IdAlmacen == cab.IdAlmacen)
                .Select(a => new { a.Ruc, a.UsuarioSol, a.ClaveSol })
                .FirstOrDefaultAsync();
            if (alm == null || string.IsNullOrWhiteSpace(alm.Ruc) ||
                string.IsNullOrWhiteSpace(alm.UsuarioSol) || string.IsNullOrWhiteSpace(alm.ClaveSol))
                return BadRequest("Credenciales SUNAT incompletas para el almacén.");

            // 4) Nombre SUNAT
            string tipo = MapTipoSunat(cab.IdTipoDocumento); // "01","03","07","08"
            string serie = cab.Serie ?? "";
            string numero = cab.Numero.ToString("D8");
            string baseName = $"{alm.Ruc}-{tipo}-{serie}-{numero}";

            // 5) Guardar XML (debe estar FIRMAdo; si no, SUNAT fallará 1032/1033)
            var tmpDir = Path.Combine(Path.GetTempPath(), "sunat-retry");
            Directory.CreateDirectory(tmpDir);
            var xmlPath = Path.Combine(tmpDir, baseName + ".xml");
            await System.IO.File.WriteAllTextAsync(xmlPath, comp.Xml);

            // 6) Comprimir y enviar (tu servicio)
            var zipPath = _facturacionService.ComprimirArchivo(xmlPath);
            if (!System.IO.File.Exists(zipPath))
                return StatusCode(500, "No se pudo comprimir el XML.");

            if (modo.Equals("bill", StringComparison.OrdinalIgnoreCase))
            {
                var resp = await _facturacionService.EnviarFacturaAsync(zipPath, alm.UsuarioSol!, alm.ClaveSol!);

                comp.EnviadoSunat = resp.exito;
                comp.RespuestaSunat = resp.mensaje;
                comp.TicketSunat = string.IsNullOrWhiteSpace(resp.ticket) ? null : resp.ticket;
                comp.FechaEnvio = DateTime.Now;
                comp.FechaRespuestaSunat = DateTime.Now;

                await _context.SaveChangesAsync();

                return resp.exito
                    ? Ok($"Aceptado: {resp.mensaje}")
                    : BadRequest($"Rechazado: {resp.mensaje}");
            }
            else if (modo.Equals("resumen", StringComparison.OrdinalIgnoreCase))
            {
                comp.EnviadoSunat = false; // queda pendiente para RC
                await _context.SaveChangesAsync();
                return Ok("Marcado como pendiente para incluir en el Resumen Diario.");
            }

            return BadRequest("Modo inválido. Usa 'bill' o 'resumen'.");
        }

        private static string MapTipoSunat(int idTipoDocumento) => idTipoDocumento switch
        {
            1 or 5 => "03", // Boleta / Boleta M
            2 or 6 => "01", // Factura / Factura M
            7 => "07",  // Nota de Crédito
            8 => "08",  // Nota de Débito
            _ => "00"
        };

        [HttpPost("regenerar-y-enviar/{idComprobante:int}")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public async Task<IActionResult> RegenerarYEnviar(int idComprobante)
        {
            // 1) Cabecera y detalles
            var cab = await _context.ComprobanteDeVentaCabeceras
                .Include(c => c.ComprobanteDeVentaDetalles)
                .Include(c => c.IdTipoDocumentoNavigation)
                .FirstOrDefaultAsync(c => c.IdComprobante == idComprobante);
            if (cab == null) return NotFound("Cabecera no encontrada.");

            // 2) Almacén (RUC, SOL)
            var alm = await _context.Almacens
                .Where(a => a.IdAlmacen == cab.IdAlmacen)
                .Select(a => new { a.Ruc, a.RazonSocial, a.Direccion, a.Ubigeo, a.Dpd, a.UsuarioSol, a.ClaveSol })
                .FirstOrDefaultAsync();
            if (alm == null || string.IsNullOrWhiteSpace(alm.Ruc) ||
                string.IsNullOrWhiteSpace(alm.UsuarioSol) || string.IsNullOrWhiteSpace(alm.ClaveSol))
                return BadRequest("Configuración SUNAT incompleta para el almacén.");

            // 3) Mapear a tu DTO (como en tu GenerarResumenDiario)
            var dpd = (alm.Dpd ?? "").Split(new[] { '-', ',' }, StringSplitOptions.RemoveEmptyEntries);
            string distrito = dpd.Length > 0 ? dpd[0].Trim() : "";
            string provincia = dpd.Length > 1 ? dpd[1].Trim() : "";
            string departamento = dpd.Length > 2 ? dpd[2].Trim() : "";

            var dto = new Models.SUNAT.DTOs.ComprobanteCabeceraDTO
            {
                IdComprobante = cab.IdComprobante,
                TipoDocumento = cab.IdTipoDocumentoNavigation.Abreviatura, // si tu generador espera "01/03", cámbialo aquí
                Serie = cab.Serie,
                Numero = cab.Numero,
                FechaEmision = cab.Fecha,
                HoraEmision = cab.Fecha.TimeOfDay,
                RucEmisor = alm.Ruc ?? "",
                RazonSocialEmisor = alm.RazonSocial ?? "",
                DireccionEmisor = alm.Direccion ?? "",
                UbigeoEmisor = alm.Ubigeo ?? "",
                DepartamentoEmisor = departamento,
                ProvinciaEmisor = provincia,
                DistritoEmisor = distrito,
                DocumentoCliente = cab.Dniruc ?? "",
                TipoDocumentoCliente = (cab.Dniruc != null && cab.Dniruc.Length == 11) ? "6" : "1",
                NombreCliente = cab.Nombre,
                DireccionCliente = cab.Direccion ?? "",
                SubTotal = cab.SubTotal,
                Igv = cab.Igv,
                Total = cab.Total,
                Detalles = cab.ComprobanteDeVentaDetalles.Select(d => new Models.SUNAT.DTOs.ComprobanteDetalleDTO
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

            // 4) Rutas y nombre SUNAT
            var tipo = MapTipoSunat(cab.IdTipoDocumento);
            var baseName = $"{alm.Ruc}-{tipo}-{cab.Serie}-{cab.Numero:D8}";
            var tmpDir = Path.Combine(Path.GetTempPath(), "sunat-retry");
            Directory.CreateDirectory(tmpDir);
            var xmlPath = Path.Combine(tmpDir, baseName + ".xml");

            // 5) Generar y firmar con TU servicio
            var gen = await _facturacionService.GenerarYFirmarFacturaAsync(dto, xmlPath);
            if (!gen.exito) return BadRequest($"Error al firmar: {gen.mensaje}");

            // 6) Guardar/actualizar en tabla Comprobantes
            var comp = await _context.Comprobantes.FirstOrDefaultAsync(c => c.IdComprobante == idComprobante);
            var xmlFirmado = await System.IO.File.ReadAllTextAsync(xmlPath);

            if (comp == null)
            {
                comp = new Comprobante
                {
                    IdComprobante = idComprobante,
                    Hash = gen.hash ?? string.Empty,
                    Xml = xmlFirmado,
                    EnviadoSunat = false,
                    Estado = true
                };
                _context.Comprobantes.Add(comp);
            }
            else
            {
                comp.Xml = xmlFirmado;
                comp.Hash = gen.hash ?? comp.Hash;
                comp.EnviadoSunat = false;
                comp.Estado = true;
                comp.RespuestaSunat = null;
                comp.TicketSunat = null;
                comp.FechaEnvio = null;
                comp.FechaRespuestaSunat = null;
            }
            await _context.SaveChangesAsync();

            // 7) Comprimir y enviar
            var zipPath = _facturacionService.ComprimirArchivo(xmlPath);
            var resp = await _facturacionService.EnviarFacturaAsync(zipPath, alm.UsuarioSol!, alm.ClaveSol!);

            comp.EnviadoSunat = resp.exito;
            comp.RespuestaSunat = resp.mensaje;
            comp.FechaEnvio = DateTime.Now;
            comp.FechaRespuestaSunat = DateTime.Now;
            comp.TicketSunat = string.IsNullOrWhiteSpace(resp.ticket) ? null : resp.ticket;
            await _context.SaveChangesAsync();

            return resp.exito ? Ok($"Aceptado: {resp.mensaje}") : BadRequest($"Rechazado: {resp.mensaje}");
        }

        [HttpPost("generar-resumen")]
        public async Task<IActionResult> GenerarResumenDiario([FromQuery] DateTime fecha, [FromQuery] int idAlmacen)
        {
            var datosAlmacen = await _context.Almacens
                .Where(a => a.IdAlmacen == idAlmacen)
                .Select(a => new { a.Nombre, a.Ruc, a.RazonSocial, a.Direccion, a.Ubigeo, a.Dpd, a.UsuarioSol, a.ClaveSol })
                .FirstOrDefaultAsync();

            if (datosAlmacen == null)
                return BadRequest("Almacén no encontrado");

            if (string.IsNullOrWhiteSpace(datosAlmacen.Ruc) ||
                string.IsNullOrWhiteSpace(datosAlmacen.UsuarioSol) ||
                string.IsNullOrWhiteSpace(datosAlmacen.ClaveSol))
                return BadRequest("Configuración SUNAT incompleta para el almacén");

            var pendientes = await (from c in _context.ComprobanteDeVentaCabeceras
                                    join f in _context.Comprobantes on c.IdComprobante equals f.IdComprobante into cf
                                    from f in cf.DefaultIfEmpty()
                                    where c.IdAlmacen == idAlmacen
                                          && c.Fecha.Date == fecha.Date
                                          && (c.IdTipoDocumento == 1 || c.IdTipoDocumento == 5)
                                          && (f == null || f.EnviadoSunat == false || f.EnviadoSunat == null || f.Estado == false)
                                    select c.IdComprobante)
                                   .ToListAsync();

            if (!pendientes.Any())
                return BadRequest("No hay comprobantes pendientes");

            var lista = new List<GSCommerceAPI.Models.SUNAT.DTOs.ComprobanteCabeceraDTO>();

            var dpdParts = (datosAlmacen.Dpd ?? "").Split(new[] { '-', ',' }, StringSplitOptions.RemoveEmptyEntries);
            string distrito = dpdParts.Length > 0 ? dpdParts[0].Trim() : string.Empty;
            string provincia = dpdParts.Length > 1 ? dpdParts[1].Trim() : string.Empty;
            string departamento = dpdParts.Length > 2 ? dpdParts[2].Trim() : string.Empty;

            foreach (var idComprobante in pendientes)
            {
                var cab = await _context.ComprobanteDeVentaCabeceras
                    .Include(c => c.ComprobanteDeVentaDetalles)
                    .Include(c => c.IdTipoDocumentoNavigation)
                    .FirstOrDefaultAsync(c => c.IdComprobante == idComprobante);

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
                    DepartamentoEmisor = departamento,
                    ProvinciaEmisor = provincia,
                    DistritoEmisor = distrito,
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