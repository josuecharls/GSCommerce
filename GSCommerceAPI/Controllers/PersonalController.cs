using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonalController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public PersonalController(SyscharlesContext context)
        {
            _context = context;
        }

        // GET: api/personal (Obtener todos los registros)
        [HttpGet]
        public async Task<IActionResult> GetPersonals(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var query = _context.Personals
                .Include(p => p.IdAlmacenNavigation)
                .Where(p => p.Estado)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Nombres.Contains(search) || p.Apellidos.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var personalList = await query
                .OrderBy(p => p.IdPersonal) // 🟢 ordenamiento obligatorio antes de paginar
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PersonalDTO
                {
                    IdPersonal = p.IdPersonal,
                    Nombres = p.Nombres,
                    Apellidos = p.Apellidos,
                    Cargo = p.Cargo,
                    IdAlmacen = p.IdAlmacen,
                    IdAlmacenNavigation = p.IdAlmacenNavigation == null ? null : new AlmacenDTO
                    {
                        IdAlmacen = p.IdAlmacenNavigation.IdAlmacen,
                        Nombre = p.IdAlmacenNavigation.Nombre
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                Data = personalList
            });
        }

        // GET: api/personal/numero (Obtener un registro por ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<Personal>> GetPersonal(int id)
        {
            var personal = await _context.Personals.FindAsync(id);
            if (personal == null)
            {
                return NotFound();
            }
            return personal;
        }

        //GET: api/personal/foto/numero (Obtener la foto de un registro por ID)
        [HttpGet("foto/{id}")]
        public async Task<IActionResult> GetFoto(int id)
        {
            var personal = await _context.Personals.FindAsync(id);
            if (personal == null || personal.Foto == null)
                return NotFound("No se encontró la foto");

            // Asegurar que se devuelva como imagen directamente
            return new FileContentResult(personal.Foto, "image/jpeg"); // O "image/png" según el tipo de imagen
        }

        [HttpGet("vendedores-por-almacen/{idAlmacen}")]
        public async Task<IActionResult> ObtenerVendedoresPorAlmacen(int idAlmacen)
        {
            var vendedores = await _context.Personals
                .Where(p => p.Cargo == "VENDEDOR" && p.IdAlmacen == idAlmacen && p.Estado)
                .Select(p => new PersonalDTO
                {
                    IdPersonal = p.IdPersonal,
                    Nombres = p.Nombres,
                    Apellidos = p.Apellidos,
                    Cargo = p.Cargo,
                    IdAlmacen = p.IdAlmacen
                }).ToListAsync();

            return Ok(vendedores);
        }


        // POST: api/personal (Crear un nuevo registro)
        [HttpPost]
        public async Task<ActionResult<Personal>> CreatePersonal(Personal personal)
        {
            _context.Personals.Add(personal);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPersonal), new { id = personal.IdPersonal }, personal);
        }

        [HttpPost("UploadFoto")]
        public async Task<IActionResult> SubirFoto([FromBody] PersonalDTO personalDto)
        {
            if (string.IsNullOrEmpty(personalDto.Foto))
                return BadRequest("No se ha enviado ninguna imagen");

            try
            {
                // Convertir de Base64 a byte[]
                byte[] fotoBytes = Convert.FromBase64String(personalDto.Foto);

                // Aquí se convierte PersonalDTO a Personal si es necesario
                var personal = new Personal
                {
                    IdPersonal = personalDto.IdPersonal,
                    Nombres = personalDto.Nombres,
                    Apellidos = personalDto.Apellidos,
                    DocIdentidad = personalDto.DocIdentidad,
                    NumeroDocIdentidad = personalDto.NumeroDocIdentidad,
                    Ruc = personalDto.Ruc,
                    Sexo = personalDto.Sexo,
                    FechaNacimiento = personalDto.FechaNacimiento,
                    Direccion = personalDto.Direccion,
                    Telefono = personalDto.Telefono,
                    Celular = personalDto.Celular,
                    Email = personalDto.Email,
                    EstadoCivil = personalDto.EstadoCivil,
                    TipoEmpleado = personalDto.TipoEmpleado,
                    LugarNacimiento = personalDto.LugarNacimiento,
                    Nacionalidad = personalDto.Nacionalidad,
                    GradoInstruccion = personalDto.GradoInstruccion,
                    Especialidad = personalDto.Especialidad,
                    IdAlmacen = personalDto.IdAlmacen,
                    Cargo = personalDto.Cargo,
                    FechaIngreso = personalDto.FechaIngreso,
                    FechaRegistro = personalDto.FechaRegistro,
                    Foto = fotoBytes, // Ahora la imagen está en byte[]
                    Estado = personalDto.Estado
                };

                // Aquí guardarías `personal` en la base de datos usando Entity Framework
                //await _context.Personal.AddAsync(personal);
                await _context.SaveChangesAsync();

                return Ok(new { Mensaje = "Imagen recibida y procesada correctamente", Tamaño = fotoBytes.Length });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al procesar la imagen: {ex.Message}");
            }
        }

        //PUT: para modificar solo la foto de un registro
        [HttpPut("UploadFoto/{id}")]
        public async Task<IActionResult> ModificarFoto(int id, [FromBody] PersonalDTO personalDto)
        {
            if (personalDto == null || string.IsNullOrEmpty(personalDto.Foto))
                return BadRequest("No se ha enviado ninguna imagen válida");

            try
            {
                // Validar si la cadena es un Base64 válido antes de convertirla
                if (!IsValidBase64(personalDto.Foto))
                    return BadRequest("La imagen enviada no es un Base64 válido.");

                byte[] fotoBytes = Convert.FromBase64String(personalDto.Foto);

                var personal = await _context.Personals.FindAsync(id);
                if (personal == null)
                    return NotFound("No se encontró el personal");

                personal.Foto = fotoBytes; // Guardamos la foto en la base de datos
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

        // Método para validar Base64
        private static bool IsValidBase64(string base64String)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64String.Length]);
            return Convert.TryFromBase64String(base64String, buffer, out _);
        }

        // PUT: api/personal/5 (Actualizar un registro)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePersonal(int id, Personal personal)
        {
            if (id != personal.IdPersonal)
            {
                return BadRequest();
            }

            _context.Entry(personal).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Personals.Any(e => e.IdPersonal == id))
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

        // DELETE: api/personal/5 (Eliminar un registro)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePersonal(int id)
        {
            var personal = await _context.Personals.FindAsync(id);
            if (personal == null)
            {
                return NotFound();
            }

            personal.Estado = false;
            _context.Entry(personal).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        //Metodo para verificar si un registro existe
        private bool PersonalExists(int id)
        {
            return _context.Personals.Any(e => e.IdPersonal == id);
        }
    }
}