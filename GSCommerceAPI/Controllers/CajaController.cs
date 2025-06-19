using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using GSCommerceAPI.Models.SUNAT.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

[Route("api/[controller]")]
[ApiController]
public class CajaController : ControllerBase
{
    private readonly SyscharlesContext _context;

    public CajaController(SyscharlesContext context)
    {
        _context = context;
    }

    // 1. Verifica si hay apertura pendiente
    [HttpGet("verificar/{idUsuario}/{idAlmacen}")]
    public async Task<IActionResult> VerificarApertura(int idUsuario, int idAlmacen)
    {
        var existe = await _context.AperturaCierreCajas
            .Where(c => c.IdUsuario == idUsuario && c.IdAlmacen == idAlmacen && c.Estado != "C")
            .ToListAsync();

        return Ok(existe);
    }

    // 2. Obtener datos de apertura para una fecha
    [HttpGet("apertura")]
    public async Task<IActionResult> ObtenerApertura([FromQuery] int idUsuario, [FromQuery] int idAlmacen, [FromQuery] string fecha)
    {
        try
        {
            if (!DateOnly.TryParse(fecha, out var fechaDate))
                return BadRequest("Formato de fecha inválido. Use YYYY-MM-DD");

            var datos = await _context.AperturaCierreCajas
                .Where(c => c.IdUsuario == idUsuario &&
                           c.IdAlmacen == idAlmacen &&
                           c.Fecha == fechaDate)
                .Include(a => a.IdUsuarioNavigation)
                .ThenInclude(u => u.IdPersonalNavigation)
                .Select(a => new AperturaCierreCajaDTO
                {
                    IdAperturaCierre = a.IdAperturaCierre,
                    IdUsuario = a.IdUsuario,
                    IdAlmacen = a.IdAlmacen,
                    Fecha = a.Fecha,
                    FondoFijo = a.FondoFijo,
                    SaldoInicial = a.SaldoInicial,
                    VentaDia = a.VentaDia,
                    Ingresos = a.Ingresos,
                    Egresos = a.Egresos,
                    SaldoFinal = a.SaldoFinal,
                    Estado = a.Estado,
                    ObservacionApertura = a.ObservacionApertura,
                    ObservacionCierre = a.ObservacionCierre,
                    NombreUsuario = a.IdUsuarioNavigation.Nombre,
                    NombreCajero = a.IdUsuarioNavigation.IdPersonalNavigation.Nombres + " " +
                                   a.IdUsuarioNavigation.IdPersonalNavigation.Apellidos
                })
                .FirstOrDefaultAsync();

            if (datos == null)
                return NotFound("No se encontró apertura para la fecha especificada");

            return Ok(datos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }


    // 3. Obtener última apertura anterior
    [HttpGet("anterior/{idUsuario}/{idAlmacen}/{fecha}")]
    public async Task<IActionResult> ObtenerAnterior(int idUsuario, int idAlmacen, string fecha)
    {
        if (!DateOnly.TryParse(fecha, out var fechaDate))
            return BadRequest("Formato de fecha inválido. Use YYYY-MM-DD");

        var anterior = await _context.AperturaCierreCajas
            .Where(c => c.IdUsuario == idUsuario && c.IdAlmacen == idAlmacen && c.Fecha < fechaDate)
            .OrderByDescending(c => c.Fecha)
            .FirstOrDefaultAsync();

        return Ok(anterior);
    }

    // 4. Obtener apertura en fecha posterior
    [HttpGet("siguiente/{idUsuario}/{idAlmacen}/{fecha}")]
    public async Task<IActionResult> ObtenerSiguiente(int idUsuario, int idAlmacen, DateOnly fecha)
    {
        var siguiente = await _context.AperturaCierreCajas
            .Where(c => c.IdUsuario == idUsuario && c.IdAlmacen == idAlmacen && c.Fecha > fecha)
            .OrderBy(c => c.Fecha)
            .FirstOrDefaultAsync();

        return Ok(siguiente);
    }

    // 5. Registrar apertura de caja
    [HttpPost("apertura")]
    public async Task<IActionResult> RegistrarApertura([FromBody] AperturaCierreCajaDTO aperturaDto)
    {
        if (aperturaDto == null)
            return BadRequest("Datos de apertura inválidos.");

        // Validar campos requeridos
        if (aperturaDto.IdUsuario <= 0 || aperturaDto.IdAlmacen <= 0)
            return BadRequest("Usuario y almacén son requeridos.");

        if (aperturaDto.FondoFijo <= 0)
            return BadRequest("El fondo fijo debe ser mayor que cero.");

        // Verificar si existe una apertura pendiente en otra fecha
        var pendiente = await _context.AperturaCierreCajas
            .FirstOrDefaultAsync(c => c.IdUsuario == aperturaDto.IdUsuario &&
                                     c.IdAlmacen == aperturaDto.IdAlmacen &&
                                     c.Estado != "C");

        if (pendiente != null)
        {
            if (pendiente.Fecha == aperturaDto.Fecha)
                return BadRequest("La caja ya está aperturada para la fecha seleccionada.");

            return BadRequest($"Existe una apertura pendiente del {pendiente.Fecha:yyyy-MM-dd}.");
        }

        // Obtener saldo final de la última apertura cerrada
        var ultimaCerrada = await _context.AperturaCierreCajas
            .Where(c => c.IdUsuario == aperturaDto.IdUsuario &&
                        c.IdAlmacen == aperturaDto.IdAlmacen &&
                        c.Estado == "C" &&
                        c.Fecha < aperturaDto.Fecha)
            .OrderByDescending(c => c.Fecha)
            .FirstOrDefaultAsync();

        var nuevaApertura = new AperturaCierreCaja
        {
            IdUsuario = aperturaDto.IdUsuario,
            IdAlmacen = aperturaDto.IdAlmacen,
            Fecha = aperturaDto.Fecha,
            FondoFijo = aperturaDto.FondoFijo,
            SaldoInicial = ultimaCerrada?.SaldoFinal ?? 0,
            Estado = aperturaDto.Estado ?? "A",
            ObservacionApertura = aperturaDto.ObservacionApertura,
            ObservacionCierre = null,
            VentaDia = 0,
            Ingresos = 0,
            Egresos = 0,
            SaldoFinal = 0
        };

        _context.AperturaCierreCajas.Add(nuevaApertura);
        await _context.SaveChangesAsync();

        return Ok(nuevaApertura);
    }

    // 6. Obtener resumen 1 (ResumenCierreDeCaja)
    [HttpGet("resumen1/{idUsuario}/{idAlmacen}/{fecha}")]
    public async Task<IActionResult> ObtenerResumen1(int idUsuario, int idAlmacen, DateOnly fecha)
    {
        var resumen1 = await _context.ResumenCierreDeCajas
            .Where(r => r.IdUsuario == idUsuario && r.IdAlmacen == idAlmacen && r.Fecha == fecha)
            .ToListAsync();

        return Ok(resumen1);
    }

    // 7. Obtener resumen 2 (v_ResumenCierreDeCaja_2)
    [HttpGet("resumen2/{idUsuario}/{idAlmacen}/{fecha}")]
    public async Task<IActionResult> ObtenerResumen2(int idUsuario, int idAlmacen, DateOnly fecha)
    {
        var resumen2 = await _context.VResumenCierreDeCaja2s
            .Where(r => r.IdUsuario == idUsuario && r.IdAlmacen == idAlmacen && r.Fecha == fecha)
            .ToListAsync();

        return Ok(resumen2);
    }

    // 8. Registrar cierre de caja
    [HttpPut("cierre/{id}")]
    public async Task<IActionResult> RegistrarCierre(int id, [FromBody] AperturaCierreCajaDTO cierre)
    {
        var actual = await _context.AperturaCierreCajas.FindAsync(id);
        if (actual == null) return NotFound();

        if (string.IsNullOrWhiteSpace(cierre.ObservacionCierre))
            return BadRequest("Observación requerida para cerrar la caja.");

        /*if (cierre.SaldoFinal == 0)
            return BadRequest("Saldo final no puede ser 0.");*/

        actual.Ingresos = cierre.Ingresos;
        actual.Egresos = cierre.Egresos;
        actual.SaldoFinal = cierre.SaldoFinal;
        actual.ObservacionCierre = cierre.ObservacionCierre;
        actual.Estado = "C";

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("ventas/{idAlmacen}/{fecha}")]
    public async Task<ActionResult<IEnumerable<VCierreVentaDiaria1>>> GetVentasDiarias(int idAlmacen, DateOnly fecha)
    {
        var lista = await _context.VCierreVentaDiaria1s
            .Where(v => v.IdAlmacen == idAlmacen && v.Fecha == fecha.ToDateTime(new TimeOnly()))
            .ToListAsync();

        return Ok(lista);
    }

    [HttpPost("liquidar")]
    public async Task<IActionResult> LiquidarCaja([FromBody] LiquidacionVentaDTO liquidacion)
    {
        var resumenes = liquidacion.Resumenes.Select(r => new ResumenCierreDeCaja
        {
            IdUsuario = r.IdUsuario,
            IdAlmacen = r.IdAlmacen,
            Fecha = r.Fecha,
            IdGrupo = r.IdGrupo,
            Grupo = r.Grupo,
            Detalle = r.Detalle,
            Monto = r.Monto,
            FechaRegistro = r.FechaRegistro ?? DateTime.Now
        }).ToList();

        _context.ResumenCierreDeCajas.AddRange(resumenes);

        var apertura = await _context.AperturaCierreCajas
            .FirstOrDefaultAsync(x => x.IdAperturaCierre == liquidacion.IdAperturaCierre);

        if (apertura == null) return NotFound("No se encontró la apertura.");

        apertura.VentaDia = liquidacion.Total;
        apertura.Estado = "L";

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("arqueo-pdf/{id}")]
    public async Task<IActionResult> GenerarArqueoPDF(int id)
    {
        // Obtener apertura
        var apertura = await _context.AperturaCierreCajas
            .Include(x => x.IdUsuarioNavigation)
            .ThenInclude(u => u.IdPersonalNavigation)
            .FirstOrDefaultAsync(x => x.IdAperturaCierre == id);

        if (apertura == null)
            return NotFound("No se encontró la apertura de caja.");

        // Obtener resumen
        var resumen = await _context.ResumenCierreDeCajas
            .Where(r => r.IdUsuario == apertura.IdUsuario && r.IdAlmacen == apertura.IdAlmacen && r.Fecha == apertura.Fecha)
            .OrderBy(r => r.IdGrupo)
            .ToListAsync();

        // Mapear al DTO
        var dto = new ArqueoCajaDTO
        {
            IdAperturaCierre = apertura.IdAperturaCierre,
            Fecha = apertura.Fecha,
            Usuario = apertura.IdUsuarioNavigation?.Nombre ?? "N/A",
            Cajero = apertura.IdUsuarioNavigation?.IdPersonalNavigation?.Nombres + " " + apertura.IdUsuarioNavigation?.IdPersonalNavigation?.Apellidos,
            SaldoInicial = apertura.SaldoInicial,
            Ingresos = apertura.Ingresos,
            Egresos = apertura.Egresos,
            VentaDia = apertura.VentaDia,
            SaldoFinal = apertura.SaldoFinal,
            ObservacionCierre = apertura.ObservacionCierre,
            Resumen = resumen.Select(r => new ResumenCierreDeCaja
            {
                Grupo = r.Grupo,
                Detalle = r.Detalle,
                Monto = r.Monto
            }).ToList()
        };

        // Generar PDF con QuestPDF
        var document = new ArqueoCajaDocument(dto);
        var pdf = document.GeneratePdf();

        return File(pdf, "application/pdf", $"ArqueoCaja_{dto.Fecha:yyyyMMdd}.pdf");
    }
}