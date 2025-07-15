using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;

namespace GSCommerceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DescuentosController : ControllerBase
{
    private readonly SyscharlesContext _context;
    public DescuentosController(SyscharlesContext context)
    {
        _context = context;
    }

    [HttpGet("{idAlmacen}")]
    public async Task<ActionResult<IEnumerable<VDescuento>>> ObtenerPorAlmacen(int idAlmacen)
    {
        var lista = await _context.VDescuentos
            .Where(d => d.IdAlmacen == idAlmacen)
            .ToListAsync();
        return Ok(lista);
    }

    [HttpGet("{idAlmacen}/{idArticulo}")]
    public async Task<ActionResult<VDescuento>> ObtenerPorArticulo(int idAlmacen, string idArticulo)
    {
        var descuento = await _context.VDescuentos
            .FirstOrDefaultAsync(d => d.IdAlmacen == idAlmacen && d.IdArticulo == idArticulo);

        if (descuento == null)
            return NotFound();

        return Ok(descuento);
    }

    [HttpPost]
    public async Task<IActionResult> AgregarDescuento([FromBody] DescuentoInput input)
    {
        var idArticuloInt = int.Parse(input.IdArticulo);
        var existe = await _context.Descuentos
            .FirstOrDefaultAsync(d => d.IdAlmacen == input.IdAlmacen && d.IdArticulo == idArticuloInt);
        if (existe != null)
            return BadRequest("El descuento ya existe");

        var nuevo = new Descuento
        {
            IdAlmacen = input.IdAlmacen,
            IdArticulo = idArticuloInt,
            Descuento1 = input.DescuentoPorc
        };
        _context.Descuentos.Add(nuevo);
        await _context.SaveChangesAsync();
        return Ok(nuevo);
    }

    [HttpPut]
    public async Task<IActionResult> ModificarDescuento([FromBody] DescuentoInput input)
    {
        var idArticuloInt = int.Parse(input.IdArticulo);
        var existe = await _context.Descuentos
            .FirstOrDefaultAsync(d => d.IdAlmacen == input.IdAlmacen && d.IdArticulo == idArticuloInt);
        if (existe == null)
            return NotFound();
        existe.Descuento1 = input.DescuentoPorc;
        await _context.SaveChangesAsync();
        return Ok(existe);
    }

    [HttpDelete("{idAlmacen}/{idArticulo}")]
    public async Task<IActionResult> EliminarDescuento(int idAlmacen, string idArticulo)
    {
        var idArticuloInt = int.Parse(idArticulo);
        var existe = await _context.Descuentos
            .FirstOrDefaultAsync(d => d.IdAlmacen == idAlmacen && d.IdArticulo == idArticuloInt);
        if (existe == null)
            return NotFound();
        _context.Descuentos.Remove(existe);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    public class DescuentoInput
    {
        public int IdAlmacen { get; set; }
        public string IdArticulo { get; set; } = string.Empty;
        public double DescuentoPorc { get; set; }
    }
}