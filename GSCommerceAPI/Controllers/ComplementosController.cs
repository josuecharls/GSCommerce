using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplementosController : ControllerBase
    {
        private readonly SyscharlesContext _context;
        public ComplementosController(SyscharlesContext context) => _context = context;

        // DTO simple para exponer ComplementoArticulo
        public class ComplementoArticuloDTO
        {
            public int IdComplemento { get; set; }
            public string Complemento { get; set; } = default!;
            public string Descripcion { get; set; } = default!;
            public string? Alias { get; set; }
            public bool Estado { get; set; }
        }

        [HttpGet("{tipo}")]
        public async Task<ActionResult<IEnumerable<ComplementoArticuloDTO>>> GetByTipo(string tipo)
        {
            var t = (tipo ?? "").Trim().ToUpper();
            var list = await _context.ComplementoArticulos
                .AsNoTracking()
                .Where(c => c.Complemento == t && c.Estado)
                .OrderBy(c => c.Descripcion)
                .Select(c => new ComplementoArticuloDTO
                {
                    IdComplemento = c.IdComplemento,
                    Complemento = c.Complemento,
                    Descripcion = c.Descripcion,
                    Alias = c.Alias,
                    Estado = c.Estado
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost]
        public async Task<ActionResult<ComplementoArticuloDTO>> Crear([FromBody] ComplementoArticuloDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Complemento) || string.IsNullOrWhiteSpace(dto.Descripcion))
                return BadRequest("Complemento y Descripcion son obligatorios.");

            var entity = new ComplementoArticulo
            {
                Complemento = dto.Complemento.Trim().ToUpper(),
                Descripcion = dto.Descripcion.Trim(),
                Alias = string.IsNullOrWhiteSpace(dto.Alias) ? null : dto.Alias.Trim(),
                Estado = true
            };

            _context.ComplementoArticulos.Add(entity);
            await _context.SaveChangesAsync();

            dto.IdComplemento = entity.IdComplemento;
            dto.Estado = entity.Estado;

            return CreatedAtAction(nameof(GetByTipo), new { tipo = entity.Complemento }, dto);
        }
    }
}