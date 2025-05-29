using GSCommerceAPI.Models.SUNAT.DTOs;
using GSCommerceAPI.Services.SUNAT;
using Microsoft.AspNetCore.Mvc;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacturacionController : ControllerBase
    {
        private readonly IFacturacionElectronicaService _facturacionService;
        private readonly IWebHostEnvironment _env;

        public FacturacionController(IFacturacionElectronicaService facturacionService, IWebHostEnvironment env)
        {
            _facturacionService = facturacionService;
            _env = env;
        }

        [HttpPost("enviar-factura")]
        public async Task<IActionResult> EnviarFactura([FromBody] EnvioFacturaRequest request)
        {
            try
            {
                // 1. Buscar XML en carpeta Facturacion
                string nombreXml = $"{request.RucEmisor}-{request.TipoDocumento}-{request.Serie}-{request.Numero:D8}.xml";
                string rutaXml = Path.Combine(Directory.GetCurrentDirectory(), "Facturacion", nombreXml);

                if (!System.IO.File.Exists(rutaXml))
                    return NotFound(new { mensaje = "No se encontró el XML firmado", ruta = rutaXml });

                // 2. Comprimir XML
                var zipPath = _facturacionService.ComprimirArchivo(rutaXml);

                // 3. Enviar a SUNAT
                var resultado = await _facturacionService.EnviarFacturaAsync(zipPath, request.UsuarioSOL, request.ClaveSOL);

                if (resultado.exito)
                    return Ok(new { mensaje = resultado.mensaje });

                return BadRequest(new { mensaje = resultado.mensaje });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al enviar factura: {ex.Message}" });
            }
        }

        [HttpPost("generar")]
        public async Task<IActionResult> GenerarXmlFactura([FromBody] ComprobanteCabeceraDTO comprobante)
        {
            try
            {
                string nombreArchivo = $"{comprobante.RucEmisor}-{comprobante.TipoDocumento}-{comprobante.Serie}-{comprobante.Numero:D8}.xml";
                string rutaXml = Path.Combine(_env.ContentRootPath, "Facturacion", nombreArchivo);

                var resultado = await _facturacionService.GenerarYFirmarFacturaAsync(comprobante, rutaXml);

                if (!resultado.exito)
                    return BadRequest(new { mensaje = resultado.mensaje });

                return Ok(new
                {
                    mensaje = resultado.mensaje,
                    hash = resultado.hash,
                    ruta = nombreArchivo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        [HttpPost("leer-cdr")]
        public async Task<IActionResult> LeerCdr([FromBody] LeerCdrRequest request)
        {
            try
            {
                // Ruta completa del ZIP de respuesta
                string rutaZipRespuesta = Path.Combine(Directory.GetCurrentDirectory(), "Facturacion", request.NombreZip);

                if (!System.IO.File.Exists(rutaZipRespuesta))
                    return NotFound(new { mensaje = "No se encontró el ZIP del CDR", ruta = rutaZipRespuesta });

                var resultado = await _facturacionService.LeerCdrAsync(rutaZipRespuesta);

                if (resultado.exito)
                {
                    return Ok(new
                    {
                        mensaje = "SUNAT aceptó el comprobante",
                        codigo = resultado.codigoRespuesta,
                        descripcion = resultado.mensaje
                    });
                }

                return BadRequest(new
                {
                    mensaje = "SUNAT rechazó o devolvió observaciones",
                    codigo = resultado.codigoRespuesta,
                    descripcion = resultado.mensaje
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al leer CDR: {ex.Message}" });
            }
        }

    }
}
