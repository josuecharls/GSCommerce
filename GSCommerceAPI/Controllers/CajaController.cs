using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using System.Globalization;
using GSCommerceAPI.Models.SUNAT.DTOs;
using System.Linq;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using System.Collections.Generic;

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
                // 204 para indicar que no existe apertura sin generar error 404
                return NoContent();

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

        if (anterior == null)
            return NoContent();

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

        if (siguiente == null)
            return NoContent();

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
            SaldoInicial = ultimaCerrada != null ? ultimaCerrada.SaldoFinal : aperturaDto.SaldoInicial,
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

        var ingresos = await _context.IngresosEgresosCabeceras
            .Where(i => i.IdUsuario == actual.IdUsuario &&
                        i.IdAlmacen == actual.IdAlmacen &&
                        DateOnly.FromDateTime(i.Fecha) == actual.Fecha &&
                        i.Naturaleza == "I" &&
                        i.Estado == "E")
            .SumAsync(i => (decimal?)i.Monto) ?? 0;

        var egresos = await _context.IngresosEgresosCabeceras
            .Where(i => i.IdUsuario == actual.IdUsuario &&
                        i.IdAlmacen == actual.IdAlmacen &&
                        DateOnly.FromDateTime(i.Fecha) == actual.Fecha &&
                        i.Naturaleza == "E" &&
                        i.Estado == "E")
            .SumAsync(i => (decimal?)i.Monto) ?? 0;

        actual.Ingresos = ingresos;
        actual.Egresos = egresos;
        actual.SaldoFinal = actual.SaldoInicial + actual.VentaDia + ingresos - egresos;
        actual.ObservacionCierre = cierre.ObservacionCierre;
        actual.Estado = "C";

        await _context.SaveChangesAsync();
        return Ok();
    }

    // 9. Listado de aperturas y cierres de caja
    [HttpGet("listado")]
    public async Task<IActionResult> ListadoAperturasCierres([FromQuery] string? fechaInicio, [FromQuery] string? fechaFin, [FromQuery] int? idAlmacen)
    {
        var query = _context.VListadoAperturaCierre1s.AsQueryable();

        if (!string.IsNullOrWhiteSpace(fechaInicio) && DateOnly.TryParse(fechaInicio, out var fi))
            query = query.Where(x => x.Fecha >= fi);

        if (!string.IsNullOrWhiteSpace(fechaFin) && DateOnly.TryParse(fechaFin, out var ff))
            query = query.Where(x => x.Fecha <= ff);

        if (idAlmacen.HasValue && idAlmacen.Value > 0)
            query = query.Where(x => x.IdAlmacen == idAlmacen.Value);

        var listado = await query
            .OrderByDescending(x => x.Fecha)
            .ThenBy(x => x.IdAlmacen)
            .ThenBy(x => x.IdUsuario)
            .ToListAsync();

        return Ok(listado);
    }

    [HttpGet("ventas/{idAlmacen}/{fecha}")]
    public async Task<ActionResult<IEnumerable<VCierreVentaDiaria1>>> GetVentasDiarias(int idAlmacen, string fecha)
    {
        if (!DateOnly.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaParsed))
            return BadRequest("Fecha inválida");

        var fechaInicio = fechaParsed.ToDateTime(new TimeOnly(0, 0, 0));
        var fechaFin = fechaParsed.ToDateTime(new TimeOnly(23, 59, 59));

        var lista = await _context.VCierreVentaDiaria1s
            .Where(v => v.IdAlmacen == idAlmacen && v.Fecha >= fechaInicio && v.Fecha <= fechaFin)
            .ToListAsync();

        return Ok(lista);
    }

    [HttpPost("liquidar")]
    public async Task<IActionResult> LiquidarCaja([FromBody] LiquidacionVentaDTO liquidacion)
    {
        var apertura = await _context.AperturaCierreCajas
            .FirstOrDefaultAsync(x => x.IdAperturaCierre == liquidacion.IdAperturaCierre);

        if (apertura == null) return NotFound("No se encontró la apertura.");

        var resumenes = new List<ResumenCierreDeCaja>();

        // 1. Ventas por tipo de documento (Boleta, Factura, Ticket)
        var ventas = await _context.ComprobanteDeVentaCabeceras
            .Include(c => c.IdTipoDocumentoNavigation)
            .Where(c => c.IdCajero == apertura.IdUsuario &&
                        c.IdAlmacen == apertura.IdAlmacen &&
                        DateOnly.FromDateTime(c.Fecha) == apertura.Fecha &&
                        c.Estado != "A")
            .Select(c => new
            {
                Tipo = c.IdTipoDocumentoNavigation.Descripcion.ToUpper(),
                c.Serie,
                c.Numero,
                Monto = c.Apagar ?? 0
            })
            .ToListAsync();

        var ventasAgrupadas = ventas
            .GroupBy(v => v.Tipo)
            .Select(g => new
            {
                Tipo = g.Key,
                Min = g.Min(v => $"{v.Serie}-{v.Numero:D8}"),
                Max = g.Max(v => $"{v.Serie}-{v.Numero:D8}"),
                Total = g.Sum(v => v.Monto)
            });

        foreach (var v in ventasAgrupadas)
        {
            string? grupo = null;
            if (v.Tipo.Contains("BOLETA")) grupo = "VENTA BOLETAS";
            else if (v.Tipo.Contains("FACTURA")) grupo = "VENTA FACTURA";
            else if (v.Tipo.Contains("TICKET")) grupo = "VENTA TICKET";

            if (grupo != null)
            {
                resumenes.Add(new ResumenCierreDeCaja
                {
                    IdUsuario = apertura.IdUsuario,
                    IdAlmacen = apertura.IdAlmacen,
                    Fecha = apertura.Fecha,
                    IdGrupo = 2,
                    Grupo = grupo,
                    Detalle = $"DESDE {v.Min} - HASTA {v.Max}",
                    Monto = v.Total,
                    FechaRegistro = DateTime.Now
                });
            }
        }

        // 2. Movimientos de ingresos y egresos
        var movimientos = await _context.IngresosEgresosCabeceras
            .Where(m => m.IdUsuario == apertura.IdUsuario &&
                        m.IdAlmacen == apertura.IdAlmacen &&
                        DateOnly.FromDateTime(m.Fecha) == apertura.Fecha &&
                        m.Estado == "E")
            .Select(m => new
            {
                m.Naturaleza,
                m.Tipo,
                m.Glosa,
                m.Monto
            })
            .ToListAsync();

        foreach (var m in movimientos)
        {
            var idGrupo = m.Naturaleza == "I" ? 3 : 5;
            resumenes.Add(new ResumenCierreDeCaja
            {
                IdUsuario = apertura.IdUsuario,
                IdAlmacen = apertura.IdAlmacen,
                Fecha = apertura.Fecha,
                IdGrupo = idGrupo,
                Grupo = m.Tipo,
                Detalle = m.Glosa,
                Monto = m.Monto,
                FechaRegistro = DateTime.Now
            });
        }

        // 3. Resumen adicional (tarjeta, NC) recibido desde la petición
        if (liquidacion.Resumenes != null)
        {
            var adicionales = liquidacion.Resumenes
                .Where(r => r.IdGrupo == 6 || r.IdGrupo == 7)
                .Select(r => new ResumenCierreDeCaja
                {
                    IdUsuario = apertura.IdUsuario,
                    IdAlmacen = apertura.IdAlmacen,
                    Fecha = apertura.Fecha,
                    IdGrupo = r.IdGrupo,
                    Grupo = r.Grupo,
                    Detalle = (r.Detalle ?? string.Empty).Trim(),
                    Monto = r.Monto,
                    FechaRegistro = DateTime.Now
                });
            resumenes.AddRange(adicionales);
        }

        // Consolidar por grupo y detalle
        resumenes = resumenes
            .GroupBy(r => new { r.IdUsuario, r.IdAlmacen, r.Fecha, r.IdGrupo, r.Grupo, r.Detalle })
            .Select(g => new ResumenCierreDeCaja
            {
                IdUsuario = g.Key.IdUsuario,
                IdAlmacen = g.Key.IdAlmacen,
                Fecha = g.Key.Fecha,
                IdGrupo = g.Key.IdGrupo,
                Grupo = g.Key.Grupo,
                Detalle = g.Key.Detalle,
                Monto = g.Sum(x => x.Monto),
                FechaRegistro = DateTime.Now
            }).ToList();

        _context.ResumenCierreDeCajas.AddRange(resumenes);

        apertura.VentaDia = liquidacion.Total;
        apertura.Estado = "L";

        await _context.SaveChangesAsync();
        return Ok();
    }
    // 10. Generar PDF de arqueo de caja
    [HttpGet("arqueo-pdf/{id}")]
    public async Task<IActionResult> GenerarArqueoPDF(int id)
    {
        // Obtener apertura
        var apertura = await _context.AperturaCierreCajas
            .Include(x => x.IdUsuarioNavigation)
            .ThenInclude(u => u.IdPersonalNavigation)
            .Include(x => x.IdAlmacenNavigation)
            .FirstOrDefaultAsync(x => x.IdAperturaCierre == id);

        if (apertura == null)
            return NotFound("No se encontró la apertura de caja.");

        // Obtener resumen
        var resumen = await _context.ResumenCierreDeCajas
            .Where(r => r.IdUsuario == apertura.IdUsuario && r.IdAlmacen == apertura.IdAlmacen && r.Fecha == apertura.Fecha)
            .OrderBy(r => r.IdGrupo)
            .ToListAsync();

        decimal MontoPorGrupo(params string[] nombres) =>
            resumen
                .Where(r => nombres.Any(n => r.Grupo.StartsWith(n, StringComparison.OrdinalIgnoreCase)))
                .Sum(r => r.Monto);

        var ventaTarjeta = MontoPorGrupo("VENTA TARJETA/ONLINE");
        var ventaNC = MontoPorGrupo("VENTA POR N.C.");
        var otrosIngresos = resumen.Where(r => r.IdGrupo == 3).Sum(r => r.Monto);
        var gastosDia = resumen.Where(r => r.IdGrupo == 5 && r.Grupo.StartsWith("GASTO", StringComparison.OrdinalIgnoreCase)).Sum(r => r.Monto);
        var transferenciasDia = resumen.Where(r => r.IdGrupo == 5 && r.Grupo.StartsWith("TRANSFERENCIA", StringComparison.OrdinalIgnoreCase)).Sum(r => r.Monto);
        var pagosProveedores = resumen.Where(r => r.IdGrupo == 5 && r.Grupo.StartsWith("PAGO", StringComparison.OrdinalIgnoreCase)).Sum(r => r.Monto);
        var ventasDelDia = MontoPorGrupo("VENTA BOLETAS", "VENTA FACTURA", "VENTA TICKET");

        var ventaEfectivo = ventasDelDia - ventaTarjeta - ventaNC;
        var saldoFinal = apertura.SaldoInicial + ventaEfectivo + otrosIngresos - gastosDia - transferenciasDia - pagosProveedores;

        // Mapear al DTO
        var dto = new ArqueoCajaDTO
        {
            IdAperturaCierre = apertura.IdAperturaCierre,
            Fecha = apertura.Fecha,
            Usuario = apertura.IdUsuarioNavigation?.Nombre ?? "N/A",
            Cajero = $"{apertura.IdUsuarioNavigation?.IdPersonalNavigation?.Nombres} {apertura.IdUsuarioNavigation?.IdPersonalNavigation?.Apellidos}",
            Empresa = apertura.IdAlmacenNavigation?.RazonSocial ?? string.Empty,
            Sucursal = apertura.IdAlmacenNavigation?.Nombre ?? string.Empty,
            SaldoInicial = apertura.SaldoInicial,
            Ingresos = apertura.Ingresos,
            Egresos = apertura.Egresos,
            VentaDia = apertura.VentaDia,
            SaldoFinal = saldoFinal,
            FondoFijo = apertura.FondoFijo,
            SaldoDiaAnterior = apertura.SaldoInicial,
            VentasDelDia = ventasDelDia,
            OtrosIngresos = otrosIngresos,
            VentaTarjeta = ventaTarjeta,
            VentaNC = ventaNC,
            GastosDia = gastosDia,
            TransferenciasDia = transferenciasDia,
            PagosProveedores = pagosProveedores,
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