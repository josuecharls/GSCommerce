using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotasCreditoController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public NotasCreditoController(SyscharlesContext context)
        {
            _context = context;
        }

        [HttpPost("emitir/{tipo}")]
        public async Task<IActionResult> EmitirNotaCredito(string tipo, [FromBody] NotaCreditoRegistroDTO dto)
        {
            if (dto == null || dto.Cabecera == null || dto.Detalles == null || !dto.Detalles.Any())
                return BadRequest("Datos incompletos.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Asignar número de serie y correlativo si corresponde
                var correlativo = await _context.SerieCorrelativos
                    .Where(s => s.Serie == dto.Cabecera.Serie && s.IdAlmacen == dto.Cabecera.IdAlmacen && s.IdTipoDocumentoVenta == dto.Cabecera.IdTipoDocumento)
                    .FirstOrDefaultAsync();

                if (correlativo == null)
                    return BadRequest("No se encontró correlativo para la serie especificada.");

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
    }
}