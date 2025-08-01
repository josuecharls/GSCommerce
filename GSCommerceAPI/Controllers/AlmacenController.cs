﻿using GSCommerce.Client.Models;
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
    public class AlmacenController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public AlmacenController(SyscharlesContext context)
        {
            _context = context;
        }

 
        // GET: api/almacenes (Obtener todos los almacenes)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AlmacenDTO>>> GetAlmacens()
        {
            var almacenes = await _context.Almacens.ToListAsync();
            var lista = new List<AlmacenDTO>();

            foreach (var a in almacenes)
            {
                var key = $"MonedaAlmacen_{a.IdAlmacen}";
                var config = await _context.Configuracions.FirstOrDefaultAsync(c => c.Configuracion1 == key);

                lista.Add(new AlmacenDTO
                {
                    IdAlmacen = a.IdAlmacen,
                    Nombre = a.Nombre,
                    EsTienda = a.EsTienda,
                    Direccion = a.Direccion,
                    Dpd = a.Dpd,
                    Telefono = a.Telefono,
                    Celular = a.Celular,
                    RazonSocial = a.RazonSocial,
                    Ruc = a.Ruc,
                    Estado = a.Estado,
                    Ubigeo = a.Ubigeo,
                    Moneda = config?.Valor ?? "PEN"
                });
            }

            return Ok(lista);
        }

        // GET: api/almacen/5 (Obtener un almacén por ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<AlmacenDTO>> GetAlmacen(int id)
        {
            var almacen = await _context.Almacens.FindAsync(id);
            if (almacen == null)
            {
                return NotFound();
            }

            var key = $"MonedaAlmacen_{almacen.IdAlmacen}";
            var config = await _context.Configuracions.FirstOrDefaultAsync(c => c.Configuracion1 == key);

            var dto = new AlmacenDTO
            {
                IdAlmacen = almacen.IdAlmacen,
                Nombre = almacen.Nombre,
                EsTienda = almacen.EsTienda,
                Direccion = almacen.Direccion,
                Dpd = almacen.Dpd,
                Telefono = almacen.Telefono,
                Celular = almacen.Celular,
                RazonSocial = almacen.RazonSocial,
                Ruc = almacen.Ruc,
                Estado = almacen.Estado,
                Ubigeo = almacen.Ubigeo,
                Moneda = config?.Valor ?? "PEN"
            };

            return Ok(dto);
        }

        // POST: api/almacen (Crear un nuevo almacén)
        [HttpPost]
        public async Task<ActionResult<Almacen>> PostAlmacen(Almacen almacen)
        {
            _context.Almacens.Add(almacen);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAlmacen), new { id = almacen.IdAlmacen }, almacen);
        }

        [HttpGet("list")]
        public async Task<ActionResult> GetAlmacenList(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? search = null)
        {
            var query = _context.Almacens.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => a.Nombre.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var almacenEntities = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var almacenList = new List<AlmacenDTO>();

            foreach (var a in almacenEntities)
            {
                var key = $"MonedaAlmacen_{a.IdAlmacen}";
                var config = await _context.Configuracions.FirstOrDefaultAsync(c => c.Configuracion1 == key);

                almacenList.Add(new AlmacenDTO
                {
                    IdAlmacen = a.IdAlmacen,
                    Nombre = a.Nombre,
                    EsTienda = a.EsTienda,
                    Direccion = a.Direccion,
                    Dpd = a.Dpd,
                    Telefono = a.Telefono,
                    Celular = a.Celular,
                    RazonSocial = a.RazonSocial,
                    Ruc = a.Ruc,
                    Estado = a.Estado,
                    Moneda = config?.Valor ?? "PEN"
                });
            }

            var response = new
            {
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                Data = almacenList
            };

            return Ok(response);
        }

        // PUT: api/almacen/5 (Actualizar un almacén)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAlmacen(int id, Almacen almacen)
        {
            if (id != almacen.IdAlmacen)
            {
                return BadRequest();
            }

            _context.Entry(almacen).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AlmacenExists(id))
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

        // DELETE: api/almacen/5 (Eliminar un almacén)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlmacen(int id)
        {
            var almacen = await _context.Almacens.FindAsync(id);
            if (almacen == null)
            {
                return NotFound();
            }

            _context.Almacens.Remove(almacen);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AlmacenExists(int id)
        {
            return _context.Almacens.Any(e => e.IdAlmacen == id);
        }
    }
}