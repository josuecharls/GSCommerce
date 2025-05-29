using GSCommerceAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/auxiliares-varios")]
    [ApiController]
    public class AuxiliaresVariosController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public AuxiliaresVariosController(SyscharlesContext context)
        {
            _context = context;
        }

        [HttpGet("motivos/{tipo}")]
        public async Task<IActionResult> GetMotivosPorTipo(string tipo)
        {
            string auxiliarFiltro = tipo.ToUpper() switch
            {
                "INGRESO" => "ALMACEN INGRESO",
                "EGRESO" => "ALMACEN EGRESO",
                "TRANSFERENCIA" => "ALMACEN TRANSFERENCIA",
                _ => ""
            };

            if (string.IsNullOrWhiteSpace(auxiliarFiltro))
                return BadRequest("Tipo de movimiento inválido");

            var motivos = await _context.AuxiliarVarios
                .Where(a => a.Auxiliar == auxiliarFiltro && a.Estado)
                .Select(a => a.Descripcion)
                .ToListAsync();

            return Ok(motivos);
        }
    }
}
