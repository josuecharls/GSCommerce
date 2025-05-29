using GSCommerceAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TipoDocumentosController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public TipoDocumentosController(SyscharlesContext context)
        {
            _context = context;
        }

        // GET: api/tipodocumentos
        [HttpGet]
        public async Task<IActionResult> GetTiposDocumento()
        {
            try
            {
                var tipos = await _context.TipoDocumentoVenta // Filtra solo activos
                    .Select(t => new
                    {
                        t.IdTipoDocumentoVenta,
                        t.Descripcion,
                        t.Abreviatura,
                        t.Manual
                    })
                    .ToListAsync();

                return Ok(tipos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener tipos de documento: {ex.Message}");
            }
        }
    }
}
