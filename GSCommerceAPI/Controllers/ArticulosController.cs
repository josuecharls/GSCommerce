using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticulosController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public ArticulosController(SyscharlesContext context)
        {
            _context = context;
        }

        // GET: api/articulos (Obtener todos los artículos con paginación y búsqueda)
        [HttpGet]
        public async Task<IActionResult> GetArticulos([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var query = _context.Articulos.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => a.Descripcion.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var articuloList = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                Data = articuloList
            });
        }

        // GET: api/articulos/nuevo-id (Generar un nuevo ID de artículo)
        [HttpGet("nuevo-id")]
        public async Task<IActionResult> GetNuevoIdArticulo()
        {
            var lastArticulo = await _context.Articulos.OrderByDescending(a => a.IdArticulo).FirstOrDefaultAsync();
            int newId = lastArticulo != null ? int.Parse(lastArticulo.IdArticulo) + 1 : 1;
            return Ok(newId.ToString("D6")); // Formato 000001, 000002, etc.
        }

        // GET: api/articulos/{id} (Obtener un artículo por ID)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetArticulo(string id)
        {
            var articulo = await _context.Articulos.FindAsync(id);
            return articulo == null ? NotFound() : Ok(articulo);
        }

        // POST: api/articulos (Crear un nuevo artículo)
        [HttpPost]
        public async Task<ActionResult<Articulo>> PostArticulo(Articulo articulo)
        {
            _context.Articulos.Add(articulo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetArticulo), new { id = articulo.IdArticulo }, articulo);
        }

        // PUT: api/articulos/{id} (Actualizar un artículo)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutArticulo(string id, Articulo articulo)
        {
            if (id != articulo.IdArticulo)
            {
                return BadRequest();
            }

            _context.Entry(articulo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ArticuloExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/articulos/{id} (Eliminar un artículo)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArticulo(string id)
        {
            var articulo = await _context.Articulos.FindAsync(id);
            if (articulo == null)
            {
                return NotFound();
            }

            _context.Articulos.Remove(articulo);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/articulos/UploadFoto/{id} (Actualizar solo la imagen del artículo)
        [HttpPut("UploadFoto/{id}")]
        public async Task<IActionResult> ModificarFoto(string id, [FromBody] ArticuloDTO articuloDto)
        {
            if (articuloDto == null || string.IsNullOrEmpty(articuloDto.Foto))
                return BadRequest("No se ha enviado ninguna imagen válida");

            try
            {
                if (!IsValidBase64(articuloDto.Foto))
                    return BadRequest("La imagen enviada no es un Base64 válido.");

                byte[] fotoBytes = Convert.FromBase64String(articuloDto.Foto);

                var articulo = await _context.Articulos.FindAsync(id);
                if (articulo == null)
                    return NotFound("No se encontró el artículo");

                articulo.Foto = fotoBytes;
                await _context.SaveChangesAsync();

                return Ok(new { Mensaje = "Imagen actualizada correctamente", Tamaño = fotoBytes.Length });
            }
            catch (FormatException ex)
            {
                return BadRequest($"Error al procesar la imagen: Formato Base64 inválido - {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error inesperado: {ex.Message}");
            }
        }

        private bool ArticuloExists(string id)
        {
            return _context.Articulos.Any(e => e.IdArticulo == id);
        }

        // Método para validar Base64
        private static bool IsValidBase64(string base64String)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64String.Length]);
            return Convert.TryFromBase64String(base64String, buffer, out _);
        }
    }
}
