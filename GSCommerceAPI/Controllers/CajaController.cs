using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using System.Globalization;
using GSCommerceAPI.Models.SUNAT.DTOs;
using GSCommerceAPI.Services.Caja;
using GSCommerceAPI.Services.Reportes;
using System.Linq;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using System.Collections.Generic;

[Route("api/[controller]")]
[ApiController]
public class CajaController : ControllerBase
{
    private readonly SyscharlesContext _context;
    private readonly ICajaVentasDiariasService _cajaVentas;
    private readonly ICuadroCajaReportProvider _cuadroCajaReport;
    private readonly ICuadroCajaEmailService _cuadroCajaEmail;

    public CajaController(
        SyscharlesContext context,
        ICajaVentasDiariasService cajaVentas,
        ICuadroCajaReportProvider cuadroCajaReport,
        ICuadroCajaEmailService cuadroCajaEmail)
    {
        _context = context;
        _cajaVentas = cajaVentas;
        _cuadroCajaReport = cuadroCajaReport;
        _cuadroCajaEmail = cuadroCajaEmail;
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

        // Calcular ventas del día usando la misma lógica que CajaLiquidacion
        var fechaInicio = actual.Fecha.ToDateTime(TimeOnly.MinValue);
        var fechaFin = actual.Fecha.ToDateTime(new TimeOnly(23, 59, 59));

        var ventas = await _cajaVentas.ObtenerVentasDiariasFiltradasAsync(
            actual.IdAlmacen, actual.IdUsuario, fechaInicio, fechaFin);

        var totalesVenta = await _cajaVentas.CalcularTotalesVentaAsync(
            ventas, fechaInicio, fechaFin, actual.IdAlmacen, actual.IdUsuario);

        var ventaDia = totalesVenta.VentaDia;
        var efectivoCaja = totalesVenta.EfectivoCaja;

        actual.VentaDia = ventaDia;
        actual.Ingresos = ingresos;
        actual.Egresos = egresos;
        // Saldo final: efectivo en caja (incluye ventas anuladas con N.C. cuyo efectivo no se devolvió)
        actual.SaldoFinal = actual.SaldoInicial + efectivoCaja + ingresos - egresos;
        actual.ObservacionCierre = cierre.ObservacionCierre;
        actual.Estado = "C";

        await RecalcularSaldosPosterioresAsync(actual);

        await _context.SaveChangesAsync();
        return Ok();
    }

    // 9. Listado de aperturas y cierres de caja
    [HttpGet("listado")]
    public async Task<IActionResult> ListadoAperturasCierres([FromQuery] DateOnly? fecha, [FromQuery] int? idAlmacen)
    {
        if (fecha is null) fecha = DateOnly.FromDateTime(DateTime.Today);

        var listado = await _cajaVentas.ObtenerListadoDiaAsync(fecha.Value, idAlmacen);

        var resultado = listado
            .Select(item => new VListadoAperturaCierre1DTO
            {
                IdAperturaCierre = item.IdAperturaCierre,
                Fecha = fecha.Value,
                IdAlmacen = item.IdAlmacen,
                Nombre = item.NombreAlmacen,
                IdUsuario = item.IdUsuario,
                Cajero = item.Cajero,
                Estado = item.Estado,
                SaldoInicial = item.SaldoInicial,
                VentaDia = item.VentaDia,
                Ingresos = item.Ingresos,
                Egresos = item.Egresos,
                SaldoFinal = item.SaldoFinal
            })
            .OrderByDescending(x => x.Fecha)
            .ThenBy(x => x.IdAlmacen)
            .ThenBy(x => x.Cajero)
            .ToList();

        return Ok(resultado);
    }

    // 11. Obtener ventas diarias para un almacén y fecha
    [HttpGet("ventas/{idAlmacen}/{fecha}")]
    public async Task<ActionResult<VentasDiariasCajaResponse>> GetVentasDiarias(int idAlmacen, string fecha, [FromQuery] int? idUsuario = null)
    {
        if (!DateOnly.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaParsed))
            return BadRequest("Fecha inválida");

        var fechaInicio = fechaParsed.ToDateTime(new TimeOnly(0, 0, 0));
        var fechaFin = fechaParsed.ToDateTime(new TimeOnly(23, 59, 59));

        if (!idUsuario.HasValue || idUsuario.Value <= 0)
            return BadRequest("El cajero es requerido.");

        var respuesta = await _cajaVentas.ObtenerVentasDiariasConTotalesAsync(
            idAlmacen, idUsuario.Value, fechaInicio, fechaFin);

        return Ok(respuesta);
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
            if (v.Tipo.Contains("BOLETA M")) grupo = "VENTA BOLETA M";
            else if (v.Tipo.Contains("BOLETA")) grupo = "VENTA BOLETAS";
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
                    Detalle = $"DEL {v.Min} - AL {v.Max}",
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

        // 3. Resumen adicional
        //    a) Ventas con tarjeta/online y notas de crédito recibidas desde la petición
        if (liquidacion.Resumenes != null)
        {
            var tarjetas = liquidacion.Resumenes
                .Where(r => r.IdGrupo == 6)
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
            resumenes.AddRange(tarjetas);

            var notasCredito = liquidacion.Resumenes
                .Where(r => r.IdGrupo == 7)
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
            resumenes.AddRange(notasCredito);
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

        // Venta del día: usar el total calculado en liquidación (efectivo + tarjeta/online, con N.C. emitidas)
        apertura.VentaDia = liquidacion.Total;
        apertura.Estado = "L";

        await _context.SaveChangesAsync();
        return Ok();
    }

    // 10. Generar PDF de arqueo de caja
    [HttpGet("arqueo-pdf/{id}")]
    public async Task<IActionResult> GenerarArqueoPDF(int id)
    {
        try
        {
            // 1) APERTURA
            var apertura = await _context.AperturaCierreCajas
                .AsNoTracking()
                .Include(x => x.IdUsuarioNavigation).ThenInclude(u => u.IdPersonalNavigation)
                .Include(x => x.IdAlmacenNavigation)
                .FirstOrDefaultAsync(x => x.IdAperturaCierre == id);

            if (apertura == null)
                return NotFound("No se encontró la apertura de caja.");

            // Ventana del día (DateOnly -> DateTime range)
            var dayStart = apertura.Fecha.ToDateTime(TimeOnly.MinValue);
            var dayEnd = apertura.Fecha.ToDateTime(new TimeOnly(23, 59, 59));

            // 2) VENTAS DEL DÍA PARA CÁLCULOS
            var ventas = await _cajaVentas.ObtenerVentasDiariasFiltradasAsync(
                apertura.IdAlmacen, apertura.IdUsuario, dayStart, dayEnd);

            var totalesVenta = await _cajaVentas.CalcularTotalesVentaAsync(
                ventas, dayStart, dayEnd, apertura.IdAlmacen, apertura.IdUsuario);

            var ventaEfectivo = totalesVenta.VentaEfectivo;
            var efectivoCaja = totalesVenta.EfectivoCaja;
            var ventaTarjeta = totalesVenta.VentaTarjeta;
            var ventaNC = totalesVenta.VentaNc;
            var ventaDia = totalesVenta.VentaDia;

            var ventasTarjetaDetalle = ventas
                .Where(v => v.Estado != "A" &&
                            (v.Descripcion.StartsWith("Tarjeta") || v.Descripcion.StartsWith("Online")) &&
                            !v.Descripcion.Contains("(N.C.)", StringComparison.OrdinalIgnoreCase))
                .GroupBy(v => v.Descripcion)
                .Select(g => new { Detalle = g.Key, Monto = g.Sum(x => x.Soles) })
                .Where(g => g.Monto != 0)
                .ToList();

            // Agrupar notas de crédito canjeadas por serie y crear rangos
            var notasCreditoDetalle = ventas
                .Where(v => v.Estado != "A" &&
                            v.Descripcion.StartsWith("N.C.") &&
                            !v.Descripcion.Contains("(N.C.)", StringComparison.OrdinalIgnoreCase))
                .GroupBy(v => v.Serie)
                .Select(g => new ResumenCierreDeCaja
                {
                    IdGrupo = 1,
                    Grupo = "VENTA POR N.C.",
                    Detalle = g.Count() == 1
                        ? $"N.C. - {g.Key}-{g.First().Numero:D8}"
                        : $"DEL {g.Key}-{g.Min(x => x.Numero):D8} AL {g.Key}-{g.Max(x => x.Numero):D8}",
                    Monto = g.Sum(x => x.Soles)
                })
                .ToList();

            // 3) RESUMEN BASE (solo VENTAS) para ese día
            var resumenBase = await _context.ResumenCierreDeCajas
                .AsNoTracking()
                .Where(r => r.IdUsuario == apertura.IdUsuario
                         && r.IdAlmacen == apertura.IdAlmacen
                         && r.Fecha == apertura.Fecha)
                .Select(r => new
                {
                    r.IdGrupo,
                    Grupo = ((r.Grupo ?? "").Trim().Equals("VENTA TICKET", StringComparison.OrdinalIgnoreCase))
                                ? "VENTA BOLETA 2"
                                : (r.Grupo ?? "").Trim(),
                    Detalle = (r.Detalle ?? "").Trim(),
                    r.Monto,
                    r.Fecha
                })
                .ToListAsync();

            var resumenVentas = resumenBase
                .Where(r => r.Grupo.StartsWith("VENTA", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // 3) MOVIMIENTOS DEL DÍA (ingresos/egresos) desde IngresosEgresosCabeceras
            var movimientos = await _context.IngresosEgresosCabeceras
                .AsNoTracking()
                .Where(m => m.IdUsuario == apertura.IdUsuario
                         && m.IdAlmacen == apertura.IdAlmacen
                         && m.Fecha >= dayStart && m.Fecha <= dayEnd
                         && m.Estado == "E")
                .Select(m => new
                {
                    IdGrupo = m.Naturaleza == "I" ? 3 : 5,
                    Grupo = (m.Tipo ?? "").Trim(),   // <- Tipo = Grupo
                    Detalle = (m.Glosa ?? "").Trim(),   // <- Glosa = Detalle
                    Monto = m.Monto,                    // <- Monto de cada unidad
                    Fecha = apertura.Fecha            // unifica a DateOnly
                })
                .ToListAsync();

            // 5) SALDO DEL DÍA ANTERIOR (del mismo cajero)
            var saldoDiaAnterior = await _context.AperturaCierreCajas
                .AsNoTracking()
                .Where(a => a.IdAlmacen == apertura.IdAlmacen 
                         && a.IdUsuario == apertura.IdUsuario 
                         && a.Fecha < apertura.Fecha 
                         && a.Estado == "C")
                .OrderByDescending(a => a.Fecha)
                .Select(a => (decimal?)a.SaldoFinal)
                .FirstOrDefaultAsync() ?? apertura.SaldoInicial;

            // --- Distribución de totales ---
            string[] gruposVentaBase = { "VENTA BOLETAS", "VENTA BOLETA M", "VENTA FACTURA", "VENTA BOLETA 2" };

            var totalesVentaPorGrupo = resumenVentas
                .Where(r => gruposVentaBase.Any(g => r.Grupo.StartsWith(g, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(r => gruposVentaBase.First(g => r.Grupo.StartsWith(g, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));
            // 6) INGRESOS / EGRESOS (solo del día) — para totales de tarjetas y egresos
            var categoriasExactas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PAGO LUZ","PAGO AGUA","PAGO TELEFONO","PAGO CELULAR","PAGO ALQUILER",
            "PAGO PLANILLAS","PAGO ADELANTOS","SUNAT","FLETE","AFP","AFP-REPORTE",
            "SUNAT-REPORTE","ALQUILER-REPORTE"
        };

            var ingresos = movimientos.Where(r => r.IdGrupo == 3).Sum(r => r.Monto);

            var transferenciasDia = movimientos
                .Where(r => r.IdGrupo == 5
                         && !string.IsNullOrWhiteSpace(r.Grupo)
                         && r.Grupo.StartsWith("TRANSFERENCIA", StringComparison.OrdinalIgnoreCase))
                .Sum(r => r.Monto);

            var pagosProveedores = movimientos
                .Where(r => r.IdGrupo == 5
                         && !string.IsNullOrWhiteSpace(r.Grupo)
                         && r.Grupo.Equals("PAGO PROVEEDORES", StringComparison.OrdinalIgnoreCase))
                .Sum(r => r.Monto);

            var gastosGenerales = movimientos
                .Where(r => r.IdGrupo == 5
                         && !string.IsNullOrWhiteSpace(r.Grupo)
                         && (r.Grupo.StartsWith("GASTOS DIVERSOS", StringComparison.OrdinalIgnoreCase)
                             || categoriasExactas.Contains(r.Grupo)))
                .Sum(r => r.Monto);

            var egresos = gastosGenerales + transferenciasDia + pagosProveedores; // total egresos
            var saldoFinal = saldoDiaAnterior + efectivoCaja + ingresos - egresos; // efectivo físico en caja

            // 7) Construir DETALLE para el PDF
            var detalleHoy = new List<ResumenCierreDeCaja>();

            // Ventas TOTALES por grupo (detalle = el primero encontrado del resumen)
            foreach (var g in gruposVentaBase)
            {
                if (totalesVentaPorGrupo.ContainsKey(g))
                {
                    var primerDetalle = resumenVentas.First(r => r.Grupo.StartsWith(g, StringComparison.OrdinalIgnoreCase)).Detalle;
                    detalleHoy.Add(new ResumenCierreDeCaja
                    {
                        IdGrupo = 1,
                        Grupo = g,
                        Detalle = primerDetalle,
                        Monto = totalesVentaPorGrupo[g]
                    });
                }
            }

            // Detalle de ventas con tarjeta agrupado por método de pago
            foreach (var vt in ventasTarjetaDetalle)
            {
                detalleHoy.Add(new ResumenCierreDeCaja
                {
                    IdGrupo = 1,
                    Grupo = "VENTA TARJETA/ONLINE",
                    Detalle = vt.Detalle,
                    Monto = vt.Monto
                });
            }
            detalleHoy.AddRange(notasCreditoDetalle);

            // Agregar movimientos del día (Tipo -> Grupo, Glosa -> Detalle)
            detalleHoy.AddRange(movimientos.Select(m => new ResumenCierreDeCaja
            {
                IdGrupo = m.IdGrupo,
                Grupo = m.Grupo,
                Detalle = m.Detalle,
                Monto = m.Monto
            }));

            // Orden final: primero ventas, luego lo demás
            detalleHoy = detalleHoy
                .OrderBy(d => d.IdGrupo == 1 ? 0 : d.IdGrupo == 3 ? 1 : 2)
                .ThenBy(d => d.Grupo)
                .ToList();

            // 8) DTO PARA EL PDF
            var dto = new ArqueoCajaDTO
            {
                IdAperturaCierre = apertura.IdAperturaCierre,
                Fecha = apertura.Fecha,
                Usuario = apertura.IdUsuarioNavigation?.Nombre ?? "N/A",
                Cajero = $"{apertura.IdUsuarioNavigation?.IdPersonalNavigation?.Nombres} {apertura.IdUsuarioNavigation?.IdPersonalNavigation?.Apellidos}".Trim(),
                Empresa = apertura.IdAlmacenNavigation?.RazonSocial ?? string.Empty,
                Sucursal = apertura.IdAlmacenNavigation?.Nombre ?? string.Empty,

                SaldoInicial = saldoDiaAnterior,
                Ingresos = ingresos,
                Egresos = egresos,
                VentaDia = ventaDia,
                SaldoFinal = saldoFinal,
                FondoFijo = apertura.FondoFijo,

                SaldoDiaAnterior = saldoDiaAnterior,
                VentasDelDia = efectivoCaja,   // efectivo real en caja (incluye anuladas con N.C.)
                OtrosIngresos = ingresos,
                VentaTarjeta = ventaTarjeta,
                VentaNC = ventaNC,
                GastosDia = gastosGenerales,
                TransferenciasDia = transferenciasDia,
                PagosProveedores = pagosProveedores,

                ObservacionCierre = apertura.ObservacionCierre ?? "",
                Resumen = detalleHoy
            };

            // 9) PDF
            var pdf = new ArqueoCajaDocument(dto).GeneratePdf();
            return File(pdf, "application/pdf", $"ArqueoCaja_{dto.Fecha:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error generando PDF: {ex.Message}");
        }
    }

    [HttpPost("enviar-cuadros-email")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> EnviarCuadrosEmail([FromQuery] DateOnly? fecha)
    {
        var fechaReporte = fecha ?? DateOnly.FromDateTime(DateTime.Today);
        var ventasEfectivo = await _cuadroCajaReport.ObtenerCuadroVentasEfectivoAsync(fechaReporte);
        if (ventasEfectivo.Filas.Count == 0)
            return BadRequest($"No hay datos de caja para {fechaReporte:yyyy-MM-dd}.");

        var billetera = await _cuadroCajaReport.ObtenerBilleteraVirtualAsync(fechaReporte);
        await _cuadroCajaEmail.EnviarCuadrosAsync(ventasEfectivo, billetera);
        return Ok(new { fecha = fechaReporte, tiendas = ventasEfectivo.Filas.Count });
    }

    //desliquidar

    [HttpPut("desliquidar/{id}")]
    public async Task<IActionResult> DesliquidarCaja(int id)
    {
        var apertura = await _context.AperturaCierreCajas
            .FirstOrDefaultAsync(x => x.IdAperturaCierre == id);

        if (apertura == null)
            return NotFound("No se encontró la apertura.");

        if (apertura.Estado != "L" && apertura.Estado != "C")
            return BadRequest("La caja no está liquidada.");

        var resumenes = await _context.ResumenCierreDeCajas
            .Where(r => r.IdUsuario == apertura.IdUsuario &&
                        r.IdAlmacen == apertura.IdAlmacen &&
                        r.Fecha == apertura.Fecha)
            .ToListAsync();

        if (resumenes.Count > 0)
            _context.ResumenCierreDeCajas.RemoveRange(resumenes);

        apertura.Estado = "A";
        apertura.VentaDia = 0;
        apertura.Ingresos = 0;
        apertura.Egresos = 0;
        apertura.SaldoFinal = apertura.SaldoInicial;
        apertura.ObservacionCierre = null;

        await RecalcularSaldosPosterioresAsync(apertura);

        await _context.SaveChangesAsync();
        return Ok();
    }

    private async Task RecalcularSaldosPosterioresAsync(AperturaCierreCaja apertura)
    {
        var posteriores = await _context.AperturaCierreCajas
            .Where(c => c.IdUsuario == apertura.IdUsuario &&
                        c.IdAlmacen == apertura.IdAlmacen &&
                        c.Fecha > apertura.Fecha)
            .OrderBy(c => c.Fecha)
            .ToListAsync();

        var saldoAnterior = apertura.SaldoFinal;

        foreach (var caja in posteriores)
        {
            var saldoCalculado = saldoAnterior + caja.VentaDia + caja.Ingresos - caja.Egresos;

            if (caja.SaldoInicial != saldoAnterior || caja.SaldoFinal != saldoCalculado)
            {
                caja.SaldoInicial = saldoAnterior;
                caja.SaldoFinal = saldoCalculado;
            }

            saldoAnterior = caja.SaldoFinal;
        }
    }
}