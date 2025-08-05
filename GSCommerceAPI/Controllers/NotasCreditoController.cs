using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Services.SUNAT;
using QuestPDF.Fluent;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotasCreditoController : ControllerBase
    {
        private readonly SyscharlesContext _context;
        private readonly IFacturacionElectronicaService _facturacion;

        public NotasCreditoController(SyscharlesContext context, IFacturacionElectronicaService facturacion)
        {
            _context = context;
            _facturacion = facturacion;
        }

        [HttpPost("emitir/{tipo}")]
        public async Task<IActionResult> EmitirNotaCredito(string tipo, [FromBody] NotaCreditoRegistroDTO dto)
        {
            if (dto == null || dto.Cabecera == null || dto.Detalles == null || !dto.Detalles.Any())
                return BadRequest("Datos incompletos.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var originales = await _context.ComprobanteDeVentaDetalles
                    .Where(c => c.IdComprobante == dto.IdComprobanteOriginal)
                    .ToDictionaryAsync(c => c.IdArticulo, c => c.Cantidad);

                foreach (var det in dto.Detalles)
                {
                    if (!originales.TryGetValue(det.IdArticulo.ToString(), out var vendido))
                        return BadRequest($"Artículo {det.Descripcion} no pertenece a la venta original.");

                    if (det.Cantidad > vendido)
                        return BadRequest($"Cantidad devuelta de {det.Descripcion} supera la vendida.");
                }
                // Asignar número de serie y correlativo si corresponde
                var correlativo = await _context.SerieCorrelativos
                    .Where(s => s.Serie == dto.Cabecera.Serie &&
                                s.IdAlmacen == dto.Cabecera.IdAlmacen &&
                                s.IdTipoDocumentoVenta == dto.Cabecera.IdTipoDocumento)
                    .FirstOrDefaultAsync();

                if (correlativo == null)
                {
                    correlativo = new SerieCorrelativo
                    {
                        IdAlmacen = dto.Cabecera.IdAlmacen,
                        IdTipoDocumentoVenta = dto.Cabecera.IdTipoDocumento,
                        Serie = dto.Cabecera.Serie,
                        Correlativo = 0,
                        Estado = true
                    };

                    _context.SerieCorrelativos.Add(correlativo);
                    await _context.SaveChangesAsync();
                }

                correlativo.Correlativo += 1;
                dto.Cabecera.Numero = correlativo.Correlativo;

                // Crear entidad cabecera
                var cabecera = new NotaDeCreditoCabecera
                {
                    IdTipoDocumento = dto.Cabecera.IdTipoDocumento,
                    Serie = dto.Cabecera.Serie,
                    Numero = dto.Cabecera.Numero,
                    Fecha = dto.Cabecera.Fecha.Date,
                    Referencia = dto.Cabecera.Referencia,
                    IdMotivo = dto.Cabecera.IdMotivo,
                    IdCliente = dto.Cabecera.IdCliente,
                    Dniruc = dto.Cabecera.Dniruc,
                    Nombre = dto.Cabecera.Nombre,
                    Direccion = dto.Cabecera.Direccion,
                    SubTotal = dto.Cabecera.SubTotal,
                    Igv = dto.Cabecera.Igv,
                    Total = dto.Cabecera.Total,
                    Redondeo = dto.Cabecera.Redondeo,
                    Afavor = dto.Cabecera.AFavor,
                    IdUsuario = dto.Cabecera.IdUsuario,
                    IdAlmacen = dto.Cabecera.IdAlmacen,
                    Estado = "E",
                    FechaHoraRegistro = DateTime.Now,
                    Empleada = false
                };

                _context.NotaDeCreditoCabeceras.Add(cabecera);
                await _context.SaveChangesAsync(); // Para generar el IdNC

                // Agregar detalles
                int item = 1;
                foreach (var d in dto.Detalles)
                {
                    var detalle = new NotaDeCreditoDetalle
                    {
                        IdNc = cabecera.IdNc,
                        Item = item++,
                        IdArticulo = d.IdArticulo.ToString(),
                        Descripcion = d.Descripcion,
                        UnidadMedida = d.UnidadMedida,
                        Cantidad = d.Cantidad,
                        Precio = d.Precio,
                        PorcentajeDescuento = d.PorcentajeDescuento,
                        Total = d.Total
                    };

                    _context.NotaDeCreditoDetalles.Add(detalle);
                }

                // Marcar en el comprobante original que ya tiene NC
                var comprobante = await _context.ComprobanteDeVentaCabeceras
                    .FirstOrDefaultAsync(c => c.IdComprobante == dto.IdComprobanteOriginal);

                if (comprobante != null)
                {
                    comprobante.GeneroNc = $"{cabecera.Serie}-{cabecera.Numero.ToString("D8")}";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Enviar a SUNAT si corresponde
                var almacen = await _context.Almacens.FirstOrDefaultAsync(a => a.IdAlmacen == cabecera.IdAlmacen);
                if (almacen != null)
                {
                    var keyMoneda = $"MonedaAlmacen_{almacen.IdAlmacen}";
                    var configMoneda = await _context.Configuracions.FirstOrDefaultAsync(c => c.Configuracion1 == keyMoneda);
                    var moneda = configMoneda?.Valor ?? "PEN";

                    var dpdParts = (almacen.Dpd ?? "").Split(new[] { '-', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    string distrito = dpdParts.Length > 0 ? dpdParts[0].Trim() : string.Empty;
                    string provincia = dpdParts.Length > 1 ? dpdParts[1].Trim() : string.Empty;
                    string departamento = dpdParts.Length > 2 ? dpdParts[2].Trim() : string.Empty;

                    var comprobanteSunat = new GSCommerceAPI.Models.SUNAT.DTOs.ComprobanteCabeceraDTO
                    {
                        IdComprobante = cabecera.IdNc,
                        TipoDocumento = "07",
                        TipoNotaCredito = cabecera.IdMotivo ?? "01", // SUNAT motivo
                        Serie = cabecera.Serie,
                        Numero = cabecera.Numero,
                        FechaEmision = cabecera.Fecha,
                        HoraEmision = cabecera.Fecha.TimeOfDay,
                        Moneda = moneda,
                        RucEmisor = almacen.Ruc ?? string.Empty,
                        RazonSocialEmisor = almacen.RazonSocial ?? string.Empty,
                        DireccionEmisor = almacen.Direccion ?? string.Empty,
                        UbigeoEmisor = almacen.Ubigeo ?? string.Empty,
                        DocumentoCliente = cabecera.Dniruc ?? string.Empty,
                        TipoDocumentoCliente = (cabecera.Dniruc != null && cabecera.Dniruc.Length == 11) ? "6" : "1",
                        NombreCliente = cabecera.Nombre,
                        DireccionCliente = cabecera.Direccion ?? string.Empty,
                        DepartamentoEmisor = departamento,
                        ProvinciaEmisor = provincia,
                        DistritoEmisor = distrito,
                        SubTotal = cabecera.SubTotal,
                        Igv = cabecera.Igv,
                        Total = cabecera.Total,
                        MontoLetras = ConvertirMontoALetras(cabecera.Total, moneda),
                        Detalles = dto.Detalles.Select(d => new GSCommerceAPI.Models.SUNAT.DTOs.ComprobanteDetalleDTO
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

                    var (exito, mensajeSunat) = await _facturacion.EnviarComprobante(comprobanteSunat);
                    if (!exito)
                    {
                        return StatusCode(500, $"❌ SUNAT rechazó la nota de crédito: {mensajeSunat}");
                    }
                }

                return Ok(new
                {
                    Numero = cabecera.Numero,
                    Serie = cabecera.Serie,
                    cabecera.IdNc
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"💥 Error al emitir nota de crédito: {ex.Message}");
            }
        }


        [HttpGet("pdf/{id}")]
        public async Task<IActionResult> ObtenerPdf(int id)
        {
            var nc = await _context.NotaDeCreditoCabeceras
                .Include(n => n.NotaDeCreditoDetalles)
                .FirstOrDefaultAsync(n => n.IdNc == id);

            if (nc == null)
                return NotFound("No se encontró la nota de crédito.");

            var dto = new Models.DTOs.NotaCreditoPDFDTO
            {
                Serie = nc.Serie,
                Numero = nc.Numero,
                Fecha = DateOnly.FromDateTime(nc.Fecha),
                Referencia = nc.Referencia ?? string.Empty,
                Cliente = nc.Nombre,
                Dniruc = nc.Dniruc ?? string.Empty,
                Direccion = nc.Direccion ?? string.Empty,
                SubTotal = nc.SubTotal,
                Igv = nc.Igv,
                Total = nc.Total,
                Detalles = nc.NotaDeCreditoDetalles.Select(d => new Models.DTOs.NotaCreditoPDFDetalleDTO
                {
                    Descripcion = d.Descripcion,
                    Cantidad = d.Cantidad ?? 0,
                    Precio = d.Precio ?? 0m,
                    Total = d.Total
                }).ToList()
            };

            var document = new Models.DTOs.NotaCreditoDocument(dto);
            var pdf = document.GeneratePdf();

            return File(pdf, "application/pdf", $"NC_{dto.Serie}-{dto.Numero:D8}.pdf");
        }

        [HttpGet("pdf-por-numero/{serie}/{numero}")]
        public async Task<IActionResult> ObtenerPdfPorNumero(string serie, int numero)
        {
            var nc = await _context.NotaDeCreditoCabeceras
                .Include(n => n.NotaDeCreditoDetalles)
                .FirstOrDefaultAsync(n => n.Serie == serie && n.Numero == numero);

            if (nc == null)
                return NotFound("No se encontró la nota de crédito.");

            return await ObtenerPdf(nc.IdNc);
        }


        [HttpGet("list")]
        public async Task<IActionResult> ListarNotasCredito(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] string? dniruc,
            [FromQuery] int? idAlmacen)
        {
            var inicio = desde ?? DateTime.Today;
            var fin = hasta ?? DateTime.Today;

            var query = _context.VListadoNotaCredito1s
                .Where(n => n.Fecha >= DateOnly.FromDateTime(inicio) && n.Fecha <= DateOnly.FromDateTime(fin));

            if (!string.IsNullOrWhiteSpace(dniruc))
                query = query.Where(n => n.Dniruc.Contains(dniruc));

            if (idAlmacen.HasValue && idAlmacen.Value != 0)
                query = query.Where(n => n.IdAlmacen == idAlmacen.Value);

            var lista = await query
                .OrderByDescending(n => n.IdNc)
                .Select(n => new GSCommerce.Client.Models.NotaCreditoConsultaDTO
                {
                    IdNc = n.IdNc,
                    Serie = n.Serie,
                    Numero = n.Numero,
                    Fecha = n.Fecha.HasValue ? n.Fecha.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue,
                    Nombre = n.Nombre,
                    Dniruc = n.Dniruc,
                    Total = n.Total,
                    Estado = n.Estado,
                    Comprobante = n.Comprobante
                })
                .ToListAsync();

            return Ok(lista);
        }


        private static string ConvertirMontoALetras(decimal monto, string moneda)
        {
            var enteros = (long)Math.Floor(monto);
            var decimales = (int)Math.Round((monto - enteros) * 100);

            string letras = NumeroALetras(enteros).ToUpper();
            var sufijo = moneda == "USD" ? "DOLARES" : "SOLES";

            return $"{letras} Y {decimales:D2}/100 {sufijo}";
        }

        private static string NumeroALetras(long numero)
        {
            if (numero == 0) return "cero";
            if (numero < 0) return "menos " + NumeroALetras(Math.Abs(numero));

            string[] unidades = { "", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve" };
            string[] decenas = { "", "diez", "veinte", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa" };
            string[] centenas = { "", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos" };

            if (numero == 100) return "cien";

            string letras = "";

            if ((numero / 1000000) > 0)
            {
                letras += NumeroALetras(numero / 1000000) + ((numero / 1000000) == 1 ? " millón " : " millones ");
                numero %= 1000000;
            }

            if ((numero / 1000) > 0)
            {
                if ((numero / 1000) == 1)
                    letras += "mil ";
                else
                    letras += NumeroALetras(numero / 1000) + " mil ";
                numero %= 1000;
            }

            if ((numero / 100) > 0)
            {
                letras += centenas[numero / 100] + " ";
                numero %= 100;
            }

            if (numero > 0)
            {
                if (numero < 10)
                    letras += unidades[numero];
                else if (numero < 20)
                {
                    string[] especiales = { "diez", "once", "doce", "trece", "catorce", "quince", "dieciséis", "diecisiete", "dieciocho", "diecinueve" };
                    letras += especiales[numero - 10];
                }
                else
                {
                    letras += decenas[numero / 10];
                    if ((numero % 10) > 0)
                        letras += " y " + unidades[numero % 10];
                }
            }

            return letras.Trim();
        }
    }
}