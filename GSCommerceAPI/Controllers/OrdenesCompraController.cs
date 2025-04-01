using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/ordenes-compra")]
    public class OrdenesCompraController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public OrdenesCompraController(SyscharlesContext context)
        {
            _context = context;
        }

        // ✅ GET: api/ordenes-compra/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrdenCompra(int id)
        {
            var orden = await _context.OrdenDeCompraCabeceras
                .Include(o => o.OrdenDeCompraDetalles)
                .FirstOrDefaultAsync(o => o.IdOc == id);

            if (orden == null)
                return NotFound();

            return Ok(new
            {
                orden.IdOc,
                orden.IdProveedor,
                orden.NumeroOc,
                orden.FechaOc,
                orden.NombreProveedor,
                orden.Rucproveedor,
                orden.Glosa,
                Detalles = orden.OrdenDeCompraDetalles.Select(d => new
                {
                    d.Item,
                    d.IdArticulo,
                    d.DescripcionArticulo,
                    d.Cantidad,
                    d.CostoUnitario
                }).ToList()
            });
        }
    }
}