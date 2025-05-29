using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

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

        [HttpGet("todos")]
        public async Task<ActionResult<List<ArticuloDTO>>> ObtenerTodos()
        {
            try
            {
                var articulos = await _context.Articulos
                    .Select(a => new ArticuloDTO
                    {
                        IdArticulo = a.IdArticulo,
                        Descripcion = a.Descripcion
                    })
                    .ToListAsync();

                return Ok(articulos);
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 ERROR API ARTICULOS: " + ex.Message);
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }


        // GET: api/articulos (Obtener todos los artículos con paginación y búsqueda)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Articulo>>> GetArticulos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
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
            var lastId = await _context.Articulos
    .Select(a => a.IdArticulo)
    .ToListAsync();

            int maxId = lastId
                .Select(id => int.TryParse(id, out var num) ? num : 0)
                .Max();

            string newId = (maxId + 1).ToString("D6");
            return Ok(newId);
        }

        // GET: api/articulos/{id} (Obtener un artículo por ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<Articulo>> GetArticulo(string id)
        {
            var articulo = await _context.Articulos.FindAsync(id);
            if (articulo == null)
            {
                return NotFound();
            }
            return articulo;
        }

        [HttpGet("foto/{id}")]
        public async Task<IActionResult> GetFoto(string id)
        {
            var articulo = await _context.Articulos.FindAsync(id);
            if (articulo == null || articulo.Foto == null)
                return NotFound("No se encontró la imagen");
            return File(articulo.Foto, "image/jpeg");
        }

        // POST: api/articulos (Crear un nuevo artículo)
        [HttpPost]
        public async Task<IActionResult> CreateArticulo([FromBody] ArticuloDTO articuloDto)
        {
            if (articuloDto == null)
                return BadRequest("No se ha recibido el artículo");

            try
            {
                var articulo = new Articulo
                {
                    IdArticulo = articuloDto.IdArticulo,
                    Descripcion = articuloDto.Descripcion,
                    DescripcionCorta = articuloDto.DescripcionCorta,
                    Familia = articuloDto.Familia,
                    Linea = articuloDto.Linea,
                    Marca = articuloDto.Marca,
                    Material = articuloDto.Material,
                    Modelo = articuloDto.Modelo,
                    Color = articuloDto.Color,
                    Detalle = articuloDto.Detalle,
                    Talla = articuloDto.Talla,
                    IdProveedor = articuloDto.IdProveedor,
                    UnidadAlmacen = articuloDto.UnidadAlmacen,
                    MonedaCosteo = articuloDto.MonedaCosteo,
                    PrecioCompra = articuloDto.PrecioCompra,
                    PrecioVenta = articuloDto.PrecioVenta,
                    FechaRegistro = articuloDto.FechaRegistro,
                    Estado = articuloDto.Estado,
                    Estacion = articuloDto.Estacion,
                    CodigoBarra = !string.IsNullOrEmpty(articuloDto.CodigoBarra) ? Encoding.UTF8.GetBytes(articuloDto.CodigoBarra) : null
                    // ❌ No incluir Foto aquí aún, se sube por separado
                };

                _context.Articulos.Add(articulo);
                await _context.SaveChangesAsync();

                return Ok(articulo);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al crear artículo: {ex.Message}");
            }
        }

        [HttpPost("UploadFoto")]
        public async Task<IActionResult> SubirFoto([FromBody] ArticuloDTO dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Foto))
                return BadRequest("No se ha enviado ninguna imagen válida");

            try
            {
                if (!IsValidBase64(dto.Foto))
                    return BadRequest("Imagen Base64 inválida.");

                var articulo = await _context.Articulos.FindAsync(dto.IdArticulo);
                if (articulo == null)
                    return NotFound("Artículo no encontrado");

                var base64Data = dto.Foto.Contains(",")
                    ? dto.Foto.Substring(dto.Foto.IndexOf(",") + 1)
                    : dto.Foto;

                articulo.Foto = Convert.FromBase64String(base64Data);
                await _context.SaveChangesAsync();

                return Ok(new { Mensaje = "Imagen actualizada correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al procesar imagen: {ex.Message}");
            }
        }

        [HttpPut("UploadFoto/{id}")]
        public async Task<IActionResult> ModificarFoto(string id, [FromBody] ArticuloDTO articuloDto)
        {
            if (articuloDto == null || string.IsNullOrEmpty(articuloDto.Foto))
                return BadRequest("No se ha enviado una imagen válida");

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
            catch (Exception ex)
            {
                return BadRequest($"Error al procesar la imagen: {ex.Message}");
            }
        }

        [HttpPost("Upload")]
        public async Task<IActionResult> SubirArticulo([FromBody] ArticuloDTO articuloDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(articuloDto.Foto))
                    return BadRequest("Debe enviar una imagen en base64.");

                var nuevoArticulo = new Articulo
                {
                    IdArticulo = articuloDto.IdArticulo,
                    Descripcion = articuloDto.Descripcion,
                    DescripcionCorta = articuloDto.DescripcionCorta,
                    Familia = articuloDto.Familia,
                    Linea = articuloDto.Linea,
                    Marca = articuloDto.Marca,
                    Material = articuloDto.Material,
                    Modelo = articuloDto.Modelo,
                    Color = articuloDto.Color,
                    Detalle = articuloDto.Detalle,
                    Talla = articuloDto.Talla,
                    IdProveedor = articuloDto.IdProveedor,
                    UnidadAlmacen = articuloDto.UnidadAlmacen,
                    MonedaCosteo = articuloDto.MonedaCosteo,
                    PrecioCompra = articuloDto.PrecioCompra,
                    PrecioVenta = articuloDto.PrecioVenta,
                    FechaRegistro = articuloDto.FechaRegistro,
                    CodigoBarra = !string.IsNullOrEmpty(articuloDto.CodigoBarra) ? Encoding.UTF8.GetBytes(articuloDto.CodigoBarra) : null,
                    Estacion = articuloDto.Estacion,
                    Estado = articuloDto.Estado,
                    Foto = !string.IsNullOrEmpty(articuloDto.Foto)? Convert.FromBase64String(articuloDto.Foto.Split(',')[1]) : null
                };  

                _context.Articulos.Add(nuevoArticulo);
                await _context.SaveChangesAsync();
                return Ok(nuevoArticulo);
            }
            catch (FormatException ex)
            {
                return BadRequest("Formato de imagen inválido. Asegúrese de enviar un Base64 válido.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
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

        // GET: api/articulos/codigo/{codigo}
        [HttpGet("codigo/{codigo}")]
        public async Task<ActionResult<ArticuloDTO>> GetArticuloPorCodigo(string codigo)
        {
            var articulo = await _context.Articulos.FirstOrDefaultAsync(a => a.IdArticulo == codigo);

            if (articulo == null)
                return NotFound();

            var dto = new ArticuloDTO
            {
                IdArticulo = articulo.IdArticulo,
                Descripcion = articulo.Descripcion,
                DescripcionCorta = articulo.DescripcionCorta,
                Familia = articulo.Familia,
                Linea = articulo.Linea,
                Marca = articulo.Marca,
                Material = articulo.Material,
                Modelo = articulo.Modelo,
                Color = articulo.Color,
                Detalle = articulo.Detalle,
                Talla = articulo.Talla,
                IdProveedor = articulo.IdProveedor,
                UnidadAlmacen = articulo.UnidadAlmacen,
                MonedaCosteo = articulo.MonedaCosteo,
                PrecioCompra = articulo.PrecioCompra,
                PrecioVenta = articulo.PrecioVenta,
                //La fecha de registro es el del mismo momento que se crea
                FechaRegistro = articulo.FechaRegistro,
                Foto = null, // Puedes cargar foto aparte si quieres optimizar
                CodigoBarra = articulo.CodigoBarra != null ? Encoding.UTF8.GetString(articulo.CodigoBarra) : null,
                Estado = articulo.Estado,
                Estacion = articulo.Estacion
            };

            return Ok(dto);
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