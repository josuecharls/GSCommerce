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
        public async Task<IActionResult> GetArticulos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery(Name = "searchTerm")] string? searchTerm = null)
        {
            // Acepta ambos nombres: ?search=... o ?searchTerm=...
            var term = (searchTerm ?? search)?.Trim();

            var query = _context.Articulos
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(term))
            {
                // Soporte: buscar por código y por descripciones
                // Opcional: si teclean "123", también matchea "000123" (6 dígitos)
                string? padded = null;
                if (int.TryParse(term, out var n) && term.Length < 6)
                    padded = n.ToString("D6");

                query = query.Where(a =>
                    a.IdArticulo.Contains(term) ||
                    (padded != null && a.IdArticulo == padded) ||
                    a.Descripcion.Contains(term) ||
                    a.DescripcionCorta.Contains(term)
                // Si quieres sumar más campos, descomenta:
                // || a.Marca.Contains(term) || a.Modelo.Contains(term)
                );
            }

            var totalItems = await query.CountAsync();

            // Ordenamos para que Skip/Take sea estable y más performante
            var articuloList = await query
                .OrderBy(a => a.IdArticulo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ArticuloDTO
                {
                    IdArticulo = a.IdArticulo,
                    Descripcion = a.Descripcion,
                    DescripcionCorta = a.DescripcionCorta,
                    Familia = a.Familia,
                    Linea = a.Linea,
                    Marca = a.Marca,
                    Material = a.Material,
                    Modelo = a.Modelo,
                    Color = a.Color,
                    Detalle = a.Detalle,
                    Talla = a.Talla,
                    IdProveedor = a.IdProveedor,
                    UnidadAlmacen = a.UnidadAlmacen,
                    MonedaCosteo = a.MonedaCosteo,
                    PrecioCompra = a.PrecioCompra,
                    PrecioVenta = a.PrecioVenta,
                    FechaRegistro = a.FechaRegistro,
                    Foto = null,
                    CodigoBarra = null,
                    Estado = a.Estado,
                    Estacion = a.Estacion
                })
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
        public async Task<IActionResult> PutArticulo(string id, [FromBody] ArticuloDTO articuloDto)
        {
            if (id != articuloDto.IdArticulo)
                return BadRequest("El ID de la ruta no coincide con el artículo enviado.");

            var articulo = await _context.Articulos.FindAsync(id);
            if (articulo == null)
                return NotFound();

            articulo.Descripcion = articuloDto.Descripcion;
            articulo.DescripcionCorta = articuloDto.DescripcionCorta;
            articulo.Familia = articuloDto.Familia;
            articulo.Linea = articuloDto.Linea;
            articulo.Marca = articuloDto.Marca;
            articulo.Material = articuloDto.Material;
            articulo.Modelo = articuloDto.Modelo;
            articulo.Color = articuloDto.Color;
            articulo.Detalle = articuloDto.Detalle;
            articulo.Talla = articuloDto.Talla;
            articulo.IdProveedor = articuloDto.IdProveedor;
            articulo.UnidadAlmacen = articuloDto.UnidadAlmacen;
            articulo.MonedaCosteo = articuloDto.MonedaCosteo;
            articulo.PrecioCompra = articuloDto.PrecioCompra;
            articulo.PrecioVenta = articuloDto.PrecioVenta;
            articulo.FechaRegistro = articuloDto.FechaRegistro;
            articulo.Estado = articuloDto.Estado;
            articulo.Estacion = articuloDto.Estacion;
            articulo.CodigoBarra = !string.IsNullOrEmpty(articuloDto.CodigoBarra)
                ? Encoding.UTF8.GetBytes(articuloDto.CodigoBarra)
                : null;

            if (!string.IsNullOrWhiteSpace(articuloDto.Foto))
            {
                var base64Data = articuloDto.Foto.Contains(",")
                    ? articuloDto.Foto.Substring(articuloDto.Foto.IndexOf(",") + 1)
                    : articuloDto.Foto;
                articulo.Foto = Convert.FromBase64String(base64Data);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ArticuloExists(id))
                    return NotFound();
                throw;
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