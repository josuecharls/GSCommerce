using GSCommerceAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TipoPagosController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public TipoPagosController(SyscharlesContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var tipos = await _context.TipoPagoVenta
                .Where(t => t.Estado)
                .Select(t => new
                {
                    t.IdTipoPagoVenta,
                    t.Tipo,
                    t.Descripcion
                })
                .ToListAsync();

            return Ok(tipos);
        }
    }
}