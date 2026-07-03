using System.Globalization;
using ClosedXML.Excel;
using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers;

[ApiController]
[Route("api/metas-mensuales")]
[Authorize]
public class MetaMensualController : ControllerBase
{
    private readonly SyscharlesContext _context;

    public MetaMensualController(SyscharlesContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<MetaMensualDTO>>> ObtenerMetas(
        [FromQuery] int? anio = null,
        [FromQuery] int? mes = null,
        [FromQuery] int? idAlmacen = null)
    {
        var query = _context.MetaMensuals
            .Include(m => m.IdAlmacenNavigation)
            .Include(m => m.MetaMensualDetalles)
            .AsQueryable();

        if (anio.HasValue)
            query = query.Where(m => m.Anio == anio.Value);
        if (mes.HasValue)
            query = query.Where(m => m.Mes == mes.Value);
        if (idAlmacen.HasValue)
            query = query.Where(m => m.IdAlmacen == idAlmacen.Value);

        var metas = await query
            .OrderByDescending(m => m.Anio)
            .ThenByDescending(m => m.Mes)
            .ThenBy(m => m.IdAlmacen)
            .ToListAsync();

        // Excluir almacenes no reportables (PRINCIPAL, tiendas *2, etc.)
        metas = metas.Where(m => IncluirEnReporteMetasMensuales(m.IdAlmacenNavigation?.Nombre)).ToList();

        var resultado = new List<MetaMensualDTO>();


        foreach (var meta in metas)
        {
            // Actualizar ventas reales antes de retornar y obtener conteo de clientes
            var clientesPorFecha = await ActualizarVentasReales(meta);

            var ventaAcumulada = meta.MetaMensualDetalles
                .Where(d => d.VentaReal.HasValue)
                .Sum(d => d.VentaReal.Value);

            var porcentajeCumplimiento = meta.Meta > 0
                ? Math.Round((ventaAcumulada / meta.Meta) * 100, 1)
                : 0;

            resultado.Add(new MetaMensualDTO
            {
                IdMetaMensual = meta.IdMetaMensual,
                IdAlmacen = meta.IdAlmacen,
                NombreAlmacen = meta.IdAlmacenNavigation.Nombre,
                Anio = meta.Anio,
                Mes = meta.Mes,
                MetaMensual = Math.Round(meta.Meta, 0),
                Estado = meta.Estado,
                FechaCreacion = meta.FechaCreacion,
                FechaEnvio = meta.FechaEnvio,
                FechaAprobacion = meta.FechaAprobacion,
                Observaciones = meta.Observaciones,
                VentaAcumulada = Math.Round(ventaAcumulada, 0),
                PorcentajeCumplimiento = porcentajeCumplimiento,
                Detalles = meta.MetaMensualDetalles
                    .OrderBy(d => d.Fecha)
                    .Select(d => new MetaMensualDetalleDTO
                    {
                        IdMetaMensualDetalle = d.IdMetaMensualDetalle,
                        Fecha = d.Fecha,
                        MetaDiaria = Math.Round(d.MetaDiaria, 0),
                        VentaReal = d.VentaReal.HasValue ? Math.Round(d.VentaReal.Value, 0) : null,
                        PorcentajeCumplimiento = d.PorcentajeCumplimiento,
                        DiaSemana = (int)d.Fecha.DayOfWeek,
                        NumeroSemana = GetNumeroSemanaFromDateOnly(d.Fecha),
                        NumeroQuincena = d.Fecha.Day <= 15 ? 1 : 2,
                        NumeroClientes = clientesPorFecha.TryGetValue(d.Fecha, out var clientes) ? clientes : null
                    })
                    .ToList()
            });
        }

        return Ok(resultado);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MetaMensualDTO>> ObtenerMetaPorId(int id)
    {
        var meta = await _context.MetaMensuals
            .Include(m => m.IdAlmacenNavigation)
            .Include(m => m.MetaMensualDetalles)
            .FirstOrDefaultAsync(m => m.IdMetaMensual == id);

        if (meta == null)
            return NotFound();

        var clientesPorFecha = await ActualizarVentasReales(meta);

        var ventaAcumulada = meta.MetaMensualDetalles
            .Where(d => d.VentaReal.HasValue)
            .Sum(d => d.VentaReal.Value);

        var porcentajeCumplimiento = meta.Meta > 0
            ? Math.Round((ventaAcumulada / meta.Meta) * 100, 1)
            : 0;

        var dto = new MetaMensualDTO
        {
            IdMetaMensual = meta.IdMetaMensual,
            IdAlmacen = meta.IdAlmacen,
            NombreAlmacen = meta.IdAlmacenNavigation.Nombre,
            Anio = meta.Anio,
            Mes = meta.Mes,
            MetaMensual = Math.Round(meta.Meta, 0),
            Estado = meta.Estado,
            FechaCreacion = meta.FechaCreacion,
            FechaEnvio = meta.FechaEnvio,
            FechaAprobacion = meta.FechaAprobacion,
            Observaciones = meta.Observaciones,
            VentaAcumulada = Math.Round(ventaAcumulada, 0),
            PorcentajeCumplimiento = porcentajeCumplimiento,
            Detalles = meta.MetaMensualDetalles
                .OrderBy(d => d.Fecha)
                .Select(d => new MetaMensualDetalleDTO
                {
                    IdMetaMensualDetalle = d.IdMetaMensualDetalle,
                    Fecha = d.Fecha,
                    MetaDiaria = Math.Round(d.MetaDiaria, 0),
                    VentaReal = d.VentaReal.HasValue ? Math.Round(d.VentaReal.Value, 0) : null,
                    PorcentajeCumplimiento = d.PorcentajeCumplimiento,
                    DiaSemana = (int)d.Fecha.DayOfWeek,
                    NumeroSemana = GetNumeroSemanaFromDateOnly(d.Fecha),
                    NumeroQuincena = d.Fecha.Day <= 15 ? 1 : 2,
                    NumeroClientes = clientesPorFecha.TryGetValue(d.Fecha, out var clientes) ? clientes : null
                })
                .ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> CrearMeta([FromBody] CrearMetaMensualRequest request)
    {
        if (request == null)
            return BadRequest("Datos inválidos");

        if (request.Mes < 1 || request.Mes > 12)
            return BadRequest("Mes inválido");

        // Validar que no exista ya una meta para ese almacén/mes/año
        var existe = await _context.MetaMensuals
            .AnyAsync(m => m.IdAlmacen == request.IdAlmacen &&
                          m.Anio == request.Anio &&
                          m.Mes == request.Mes);
        if (existe)
            return BadRequest("Ya existe una meta para este almacén y período");

        // Validar que el almacén existe
        var almacen = await _context.Almacens.FindAsync(request.IdAlmacen);
        if (almacen == null)
            return NotFound("Almacén no encontrado");

        // Obtener ID del usuario actual
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized("Usuario no válido");

        // Calcular meta mensual automáticamente desde datos históricos
        var metaMensualCalculada = await CalcularMetaMensual(
            request.IdAlmacen,
            request.Anio,
            request.Mes,
            request.MesesHistoricos);

        // Calcular distribución diaria
        var detalles = await CalcularDistribucionDiaria(
            request.IdAlmacen,
            request.Anio,
            request.Mes,
            metaMensualCalculada,
            request.MesesHistoricos);

        var meta = new MetaMensual
        {
            IdAlmacen = request.IdAlmacen,
            Anio = request.Anio,
            Mes = request.Mes,
            Meta = metaMensualCalculada,
            Estado = "PENDIENTE",
            FechaCreacion = DateTime.Now,
            IdUsuarioCreacion = userId
        };

        foreach (var detalle in detalles)
        {
            meta.MetaMensualDetalles.Add(detalle);
        }

        _context.MetaMensuals.Add(meta);
        await _context.SaveChangesAsync();

        return Ok(new { IdMetaMensual = meta.IdMetaMensual, Meta = metaMensualCalculada });
    }

    [HttpPost("{id}/enviar")]
    public async Task<IActionResult> EnviarPropuesta(int id)
    {
        var meta = await _context.MetaMensuals.FindAsync(id);
        if (meta == null)
            return NotFound();

        if (meta.Estado != "PENDIENTE")
            return BadRequest("Solo se pueden enviar propuestas pendientes");

        // Validar que se envíe solo en los últimos 2 días del mes anterior o el primer día del mes
        var fechaInicioMes = new DateTime(meta.Anio, meta.Mes, 1);
        var fechaInicioPermitida = fechaInicioMes.AddDays(-7); // Penúltimo día del mes anterior
        var fechaFinPermitida = fechaInicioMes; // Primer día del mes de la meta
        var fechaActual = DateTime.Now.Date; // Solo comparar fechas, sin hora
        
        if (fechaActual < fechaInicioPermitida.Date || fechaActual > fechaFinPermitida.Date)
            return BadRequest("Las propuestas solo pueden enviarse durante los últimos 2 días del mes anterior o el primer día del mes de la meta");

        meta.Estado = "ENVIADA";
        meta.FechaEnvio = DateTime.Now;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/aprobar")]
    public async Task<IActionResult> AprobarRechazar(int id, [FromBody] AprobarMetaMensualRequest request)
    {
        if (request == null || request.IdMetaMensual != id)
            return BadRequest("Datos inválidos");

        var meta = await _context.MetaMensuals.FindAsync(request.IdMetaMensual);
        if (meta == null)
            return NotFound();

        if (meta.Estado != "ENVIADA")
            return BadRequest("Solo se pueden aprobar/rechazar propuestas enviadas");

        var userIdClaim = User.FindFirst("userId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized("Usuario no válido");

        // Verificar que sea administrador
        var cargo = User.FindFirst("Cargo")?.Value ?? string.Empty;
        if (!string.Equals(cargo, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase))
            return Forbid("Solo los administradores pueden aprobar/rechazar propuestas");

        meta.Estado = request.Aprobar ? "APROBADA" : "RECHAZADA";
        meta.IdUsuarioAprobacion = userId;
        meta.FechaAprobacion = DateTime.Now;
        meta.Observaciones = request.Observaciones;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarMetaMensual(int id, [FromBody] ActualizarMetaMensualRequest request)
    {
        if (request == null)
            return BadRequest("Datos inválidos");

        // Verificar que sea administrador
        var cargo = User.FindFirst("Cargo")?.Value ?? string.Empty;
        if (!string.Equals(cargo, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase))
            return Forbid("Solo los administradores pueden modificar metas");

        var meta = await _context.MetaMensuals
            .Include(m => m.MetaMensualDetalles)
            .FirstOrDefaultAsync(m => m.IdMetaMensual == id);
        
        if (meta == null)
            return NotFound();

        // Actualizar meta mensual (redondeada)
        meta.Meta = Math.Round(request.MetaMensual, 0);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/detalles")]
    public async Task<IActionResult> ActualizarDetalles(int id, [FromBody] ActualizarDetallesRequest request)
    {
        if (request == null || request.Detalles == null || !request.Detalles.Any())
            return BadRequest("Datos inválidos");

        // Verificar que sea administrador
        var cargo = User.FindFirst("Cargo")?.Value ?? string.Empty;
        if (!string.Equals(cargo, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase))
            return Forbid("Solo los administradores pueden modificar metas");

        var meta = await _context.MetaMensuals
            .Include(m => m.MetaMensualDetalles)
            .FirstOrDefaultAsync(m => m.IdMetaMensual == id);
        
        if (meta == null)
            return NotFound();

        // Actualizar cada detalle
        foreach (var detalleUpdate in request.Detalles)
        {
            var detalle = meta.MetaMensualDetalles.FirstOrDefault(d => d.IdMetaMensualDetalle == detalleUpdate.IdMetaMensualDetalle);
            if (detalle != null)
            {
                detalle.MetaDiaria = Math.Round(detalleUpdate.MetaDiaria, 0);
            }
        }

        // Recalcular meta mensual como suma de cuotas diarias
        meta.Meta = Math.Round(meta.MetaMensualDetalles.Sum(d => d.MetaDiaria), 0);
        
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("generar-todas")]
    public async Task<IActionResult> GenerarMetasParaTodosAlmacenes([FromBody] CrearMetaMensualRequest request)
    {
        if (request == null)
            return BadRequest("Datos inválidos");

        if (request.Mes < 1 || request.Mes > 12)
            return BadRequest("Mes inválido");

        // Obtener ID del usuario actual
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized("Usuario no válido");

        // Obtener todos los almacenes que son tiendas
        var almacenes = await _context.Almacens
            .Where(a => a.EsTienda && a.Estado)
            .ToListAsync();

        var resultados = new List<object>();

        foreach (var almacen in almacenes)
        {
            // Validar que no exista ya una meta
            var existe = await _context.MetaMensuals
                .AnyAsync(m => m.IdAlmacen == almacen.IdAlmacen &&
                              m.Anio == request.Anio &&
                              m.Mes == request.Mes);
            
            if (existe)
            {
                resultados.Add(new { Almacen = almacen.Nombre, Estado = "Ya existe", MetaMensual = 0m });
                continue;
            }

            // Calcular meta mensual automáticamente
            var metaMensualCalculada = await CalcularMetaMensual(
                almacen.IdAlmacen,
                request.Anio,
                request.Mes,
                request.MesesHistoricos);

            // Calcular distribución diaria
            var detalles = await CalcularDistribucionDiaria(
                almacen.IdAlmacen,
                request.Anio,
                request.Mes,
                metaMensualCalculada,
                request.MesesHistoricos);

            var meta = new MetaMensual
            {
                IdAlmacen = almacen.IdAlmacen,
                Anio = request.Anio,
                Mes = request.Mes,
                Meta = metaMensualCalculada,
                Estado = "PENDIENTE",
                FechaCreacion = DateTime.Now,
                IdUsuarioCreacion = userId
            };

            foreach (var detalle in detalles)
            {
                meta.MetaMensualDetalles.Add(detalle);
            }

            _context.MetaMensuals.Add(meta);
            resultados.Add(new { Almacen = almacen.Nombre, Estado = "Creada", Meta = metaMensualCalculada });
        }

        await _context.SaveChangesAsync();

        return Ok(resultados);
    }

    [HttpGet("exportar")]
    public async Task<IActionResult> ExportarExcel([FromQuery] int anio, [FromQuery] int mes)
    {
        if (mes < 1 || mes > 12)
            return BadRequest("Mes inválido");

        // Obtener todas las metas del mes para todos los almacenes
        var metas = await _context.MetaMensuals
            .Include(m => m.IdAlmacenNavigation)
            .Include(m => m.MetaMensualDetalles)
            .Where(m => m.Anio == anio && m.Mes == mes)
            .OrderBy(m => m.IdAlmacen)
            .ToListAsync();

        metas = metas.Where(m => IncluirEnReporteMetasMensuales(m.IdAlmacenNavigation?.Nombre)).ToList();

        if (!metas.Any())
            return NotFound("No se encontraron metas para el período indicado");

        // Actualizar ventas reales para todas las metas
        foreach (var meta in metas)
        {
            await ActualizarVentasReales(meta);
        }

        var cultura = CultureInfo.CreateSpecificCulture("es-PE");
        var fecha = new DateTime(anio, mes, 1);
        var nombreMes = cultura.TextInfo.ToTitleCase(fecha.ToString("MMMM", cultura));

        using var workbook = new XLWorkbook();

        // Crear una hoja por cada almacén
        foreach (var meta in metas)
        {
            // Sanitizar nombre de hoja: eliminar caracteres problemáticos
            var nombreHoja = meta.IdAlmacenNavigation.Nombre
                .Replace("*", "")
                .Replace("/", "")
                .Replace(":", "")
                .Replace("?", "");
            if (nombreHoja.Length > 31)
                nombreHoja = nombreHoja.Substring(0, 31);
            
            var hoja = workbook.Worksheets.Add(nombreHoja);

            // Encabezado principal
            hoja.Cell(1, 1).Value = "META MENSUAL";
            hoja.Range(1, 1, 1, 7).Merge();
            hoja.Cell(1, 1).Style.Font.SetBold();
            hoja.Cell(1, 1).Style.Font.FontSize = 16;
            hoja.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(230, 230, 230);

            hoja.Cell(2, 1).Value = $"Almacén: {meta.IdAlmacenNavigation.Nombre}";
            hoja.Cell(3, 1).Value = $"Período: {nombreMes} {meta.Anio}";
            hoja.Cell(4, 1).Value = $"Meta Mensual: S/ {meta.Meta:N2}";
            hoja.Cell(4, 1).Style.Font.SetBold();

            var ventaAcumulada = meta.MetaMensualDetalles
                .Where(d => d.VentaReal.HasValue)
                .Sum(d => d.VentaReal.Value);
            var porcentajeCumplimiento = meta.Meta > 0
                ? (ventaAcumulada / meta.Meta) * 100
                : 0;

            hoja.Cell(5, 1).Value = $"Venta Acumulada: S/ {ventaAcumulada:N2}";
            hoja.Cell(6, 1).Value = $"Cumplimiento: {porcentajeCumplimiento:N2}%";
            hoja.Cell(6, 1).Style.Font.SetBold();

            // Encabezados de tabla
            int fila = 8;
            hoja.Cell(fila, 1).Value = "Fecha";
            hoja.Cell(fila, 2).Value = "Día";
            hoja.Cell(fila, 3).Value = "Meta Diaria";
            hoja.Cell(fila, 4).Value = "Venta Real";
            hoja.Cell(fila, 5).Value = "Cumplimiento %";
            hoja.Cell(fila, 6).Value = "Quincena";
            hoja.Cell(fila, 7).Value = "Semana";

            var headerRange = hoja.Range(fila, 1, fila, 7);
            headerRange.Style.Font.SetBold();
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(200, 200, 200);
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Datos
            fila++;
            var filaInicioDatos = fila;
            var detallesOrdenados = meta.MetaMensualDetalles.OrderBy(d => d.Fecha).ToList();
            foreach (var detalle in detallesOrdenados)
            {
                var fechaDateTime = detalle.Fecha.ToDateTime(TimeOnly.MinValue);
                var diaSemana = fechaDateTime.ToString("dddd", cultura);
                var numeroSemana = GetNumeroSemana(fechaDateTime);
                var numeroQuincena = detalle.Fecha.Day <= 15 ? 1 : 2;

                // Fecha como objeto DateTime para permitir filtrado en Excel
                hoja.Cell(fila, 1).Value = fechaDateTime;
                hoja.Cell(fila, 2).Value = diaSemana;
                hoja.Cell(fila, 3).Value = detalle.MetaDiaria;
                hoja.Cell(fila, 4).Value = detalle.VentaReal ?? 0;
                hoja.Cell(fila, 5).Value = detalle.PorcentajeCumplimiento ?? 0;
                hoja.Cell(fila, 6).Value = $"Q{numeroQuincena}";
                hoja.Cell(fila, 7).Value = $"S{numeroSemana}";

                // Color según cumplimiento
                if (detalle.PorcentajeCumplimiento.HasValue)
                {
                    var porcentaje = detalle.PorcentajeCumplimiento.Value;
                    if (porcentaje >= 100)
                        hoja.Cell(fila, 5).Style.Font.FontColor = XLColor.Green;
                    else if (porcentaje >= 80)
                        hoja.Cell(fila, 5).Style.Font.FontColor = XLColor.Orange;
                    else
                        hoja.Cell(fila, 5).Style.Font.FontColor = XLColor.Red;
                }

                fila++;
            }
            
            // Aplicar formatos fuera del bucle para optimizar
            var filaFinDatos = fila - 1;
            if (filaFinDatos >= filaInicioDatos)
            {
                // Formato de fecha (columna 1)
                hoja.Range(filaInicioDatos, 1, filaFinDatos, 1).Style.NumberFormat.Format = "dd/MM/yyyy";
                // Formato de moneda para Meta Diaria (columna 3)
                hoja.Range(filaInicioDatos, 3, filaFinDatos, 3).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
                // Formato de moneda para Venta Real (columna 4)
                hoja.Range(filaInicioDatos, 4, filaFinDatos, 4).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
                // Formato de porcentaje para Cumplimiento (columna 5)
                hoja.Range(filaInicioDatos, 5, filaFinDatos, 5).Style.NumberFormat.Format = "#,##0.00%";
            }

            // Resumen por quincenas
            fila += 2;
            hoja.Cell(fila, 1).Value = "RESUMEN POR QUINCENAS";
            hoja.Cell(fila, 1).Style.Font.SetBold();
            hoja.Cell(fila, 1).Style.Font.FontSize = 14;
            fila++;

            hoja.Cell(fila, 1).Value = "Quincena";
            hoja.Cell(fila, 2).Value = "Meta";
            hoja.Cell(fila, 3).Value = "Venta Real";
            hoja.Cell(fila, 4).Value = "Cumplimiento %";
            var resumenHeaderRange = hoja.Range(fila, 1, fila, 4);
            resumenHeaderRange.Style.Font.SetBold();
            resumenHeaderRange.Style.Fill.BackgroundColor = XLColor.FromArgb(200, 200, 200);

            fila++;
            for (int q = 1; q <= 2; q++)
            {
                var detallesQuincena = detallesOrdenados
                    .Where(d => (d.Fecha.Day <= 15 ? 1 : 2) == q)
                    .ToList();
                var metaQuincena = detallesQuincena.Sum(d => d.MetaDiaria);
                var ventaQuincena = detallesQuincena
                    .Where(d => d.VentaReal.HasValue)
                    .Sum(d => d.VentaReal.Value);
                var porcentajeQuincena = metaQuincena > 0
                    ? (ventaQuincena / metaQuincena) * 100
                    : 0;

                hoja.Cell(fila, 1).Value = $"Q{q}";
                hoja.Cell(fila, 2).Value = metaQuincena;
                hoja.Cell(fila, 3).Value = ventaQuincena;
                hoja.Cell(fila, 4).Value = porcentajeQuincena;

                hoja.Cell(fila, 2).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
                hoja.Cell(fila, 3).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
                hoja.Cell(fila, 4).Style.NumberFormat.Format = "0.00%";

                fila++;
            }

            // Resumen por semanas
            fila += 2;
            hoja.Cell(fila, 1).Value = "RESUMEN POR SEMANAS";
            hoja.Cell(fila, 1).Style.Font.SetBold();
            hoja.Cell(fila, 1).Style.Font.FontSize = 14;
            fila++;

            hoja.Cell(fila, 1).Value = "Semana";
            hoja.Cell(fila, 2).Value = "Meta";
            hoja.Cell(fila, 3).Value = "Venta Real";
            hoja.Cell(fila, 4).Value = "Cumplimiento %";
            var semanaHeaderRange = hoja.Range(fila, 1, fila, 4);
            semanaHeaderRange.Style.Font.SetBold();
            semanaHeaderRange.Style.Fill.BackgroundColor = XLColor.FromArgb(200, 200, 200);

            fila++;
            var semanas = detallesOrdenados
                .GroupBy(d => GetNumeroSemanaFromDateOnly(d.Fecha))
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var semana in semanas)
            {
                var metaSemana = semana.Sum(d => d.MetaDiaria);
                var ventaSemana = semana
                    .Where(d => d.VentaReal.HasValue)
                    .Sum(d => d.VentaReal.Value);
                var porcentajeSemana = metaSemana > 0
                    ? (ventaSemana / metaSemana) * 100
                    : 0;

                hoja.Cell(fila, 1).Value = $"S{semana.Key}";
                hoja.Cell(fila, 2).Value = metaSemana;
                hoja.Cell(fila, 3).Value = ventaSemana;
                hoja.Cell(fila, 4).Value = porcentajeSemana;

                hoja.Cell(fila, 2).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
                hoja.Cell(fila, 3).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
                hoja.Cell(fila, 4).Style.NumberFormat.Format = "0.00%";

                fila++;
            }

            hoja.Columns().AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var nombreArchivo = $"MetasMensuales_{nombreMes}_{anio}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nombreArchivo);
    }

    [HttpGet("{id}/exportar")]
    public async Task<IActionResult> ExportarExcel(int id)
    {
        var meta = await _context.MetaMensuals
            .Include(m => m.IdAlmacenNavigation)
            .Include(m => m.MetaMensualDetalles)
            .FirstOrDefaultAsync(m => m.IdMetaMensual == id);

        if (meta == null)
            return NotFound();

        // Actualizar ventas reales
        await ActualizarVentasReales(meta);

        var cultura = CultureInfo.CreateSpecificCulture("es-PE");
        var fecha = new DateTime(meta.Anio, meta.Mes, 1);
        var nombreMes = cultura.TextInfo.ToTitleCase(fecha.ToString("MMMM", cultura));

        using var workbook = new XLWorkbook();
        // Sanitizar nombre de hoja: eliminar caracteres problemáticos
        var nombreHoja = meta.IdAlmacenNavigation.Nombre
            .Replace("*", "")
            .Replace("/", "")
            .Replace(":", "")
            .Replace("?", "");
        if (nombreHoja.Length > 31)
            nombreHoja = nombreHoja.Substring(0, 31);
        
        var hoja = workbook.Worksheets.Add(nombreHoja);

        // Encabezado principal
        hoja.Cell(1, 1).Value = "META MENSUAL";
        hoja.Range(1, 1, 1, 7).Merge();
        hoja.Cell(1, 1).Style.Font.SetBold();
        hoja.Cell(1, 1).Style.Font.FontSize = 16;
        hoja.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        hoja.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(230, 230, 230);

        hoja.Cell(2, 1).Value = $"Almacén: {meta.IdAlmacenNavigation.Nombre}";
        hoja.Cell(3, 1).Value = $"Período: {nombreMes} {meta.Anio}";
        hoja.Cell(4, 1).Value = $"Meta Mensual: S/ {meta.Meta:N2}";
        hoja.Cell(4, 1).Style.Font.SetBold();

        var ventaAcumulada = meta.MetaMensualDetalles
            .Where(d => d.VentaReal.HasValue)
            .Sum(d => d.VentaReal.Value);
        var porcentajeCumplimiento = meta.Meta > 0
            ? (ventaAcumulada / meta.Meta) * 100
            : 0;

        hoja.Cell(5, 1).Value = $"Venta Acumulada: S/ {ventaAcumulada:N2}";
        hoja.Cell(6, 1).Value = $"Cumplimiento: {porcentajeCumplimiento:N2}%";
        hoja.Cell(6, 1).Style.Font.SetBold();

        // Encabezados de tabla
        int fila = 8;
        hoja.Cell(fila, 1).Value = "Fecha";
        hoja.Cell(fila, 2).Value = "Día";
        hoja.Cell(fila, 3).Value = "Meta Diaria";
        hoja.Cell(fila, 4).Value = "Venta Real";
        hoja.Cell(fila, 5).Value = "Cumplimiento %";
        hoja.Cell(fila, 6).Value = "Quincena";
        hoja.Cell(fila, 7).Value = "Semana";

        var headerRange = hoja.Range(fila, 1, fila, 7);
        headerRange.Style.Font.SetBold();
        headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(200, 200, 200);
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Datos
        fila++;
        var filaInicioDatos = fila;
        var detallesOrdenados = meta.MetaMensualDetalles.OrderBy(d => d.Fecha).ToList();
        foreach (var detalle in detallesOrdenados)
        {
            var fechaDateTime = detalle.Fecha.ToDateTime(TimeOnly.MinValue);
            var diaSemana = fechaDateTime.ToString("dddd", cultura);
            var numeroSemana = GetNumeroSemana(fechaDateTime);
            var numeroQuincena = detalle.Fecha.Day <= 15 ? 1 : 2;

            // Fecha como objeto DateTime para permitir filtrado en Excel
            hoja.Cell(fila, 1).Value = fechaDateTime;
            hoja.Cell(fila, 2).Value = diaSemana;
            hoja.Cell(fila, 3).Value = detalle.MetaDiaria;
            hoja.Cell(fila, 4).Value = detalle.VentaReal ?? 0;
            hoja.Cell(fila, 5).Value = detalle.PorcentajeCumplimiento ?? 0;
            hoja.Cell(fila, 6).Value = $"Q{numeroQuincena}";
            hoja.Cell(fila, 7).Value = $"S{numeroSemana}";

            // Color según cumplimiento
            if (detalle.PorcentajeCumplimiento.HasValue)
            {
                var porcentaje = detalle.PorcentajeCumplimiento.Value;
                if (porcentaje >= 100)
                    hoja.Cell(fila, 5).Style.Font.FontColor = XLColor.Green;
                else if (porcentaje >= 80)
                    hoja.Cell(fila, 5).Style.Font.FontColor = XLColor.Orange;
                else
                    hoja.Cell(fila, 5).Style.Font.FontColor = XLColor.Red;
            }

            fila++;
        }
        
        // Aplicar formatos fuera del bucle para optimizar
        var filaFinDatos = fila - 1;
        if (filaFinDatos >= filaInicioDatos)
        {
            // Formato de fecha (columna 1)
            hoja.Range(filaInicioDatos, 1, filaFinDatos, 1).Style.NumberFormat.Format = "dd/MM/yyyy";
            // Formato de moneda para Meta Diaria (columna 3)
            hoja.Range(filaInicioDatos, 3, filaFinDatos, 3).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
            // Formato de moneda para Venta Real (columna 4)
            hoja.Range(filaInicioDatos, 4, filaFinDatos, 4).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
            // Formato de porcentaje para Cumplimiento (columna 5)
            hoja.Range(filaInicioDatos, 5, filaFinDatos, 5).Style.NumberFormat.Format = "#,##0.00%";
        }

        // Resumen por quincenas
        fila += 2;
        hoja.Cell(fila, 1).Value = "RESUMEN POR QUINCENAS";
        hoja.Cell(fila, 1).Style.Font.SetBold();
        hoja.Cell(fila, 1).Style.Font.FontSize = 14;
        fila++;

        hoja.Cell(fila, 1).Value = "Quincena";
        hoja.Cell(fila, 2).Value = "Meta";
        hoja.Cell(fila, 3).Value = "Venta Real";
        hoja.Cell(fila, 4).Value = "Cumplimiento %";
        var resumenHeaderRange = hoja.Range(fila, 1, fila, 4);
        resumenHeaderRange.Style.Font.SetBold();
        resumenHeaderRange.Style.Fill.BackgroundColor = XLColor.FromArgb(200, 200, 200);

        fila++;
        for (int q = 1; q <= 2; q++)
        {
            var detallesQuincena = detallesOrdenados
                .Where(d => (d.Fecha.Day <= 15 ? 1 : 2) == q)
                .ToList();
            var metaQuincena = detallesQuincena.Sum(d => d.MetaDiaria);
            var ventaQuincena = detallesQuincena
                .Where(d => d.VentaReal.HasValue)
                .Sum(d => d.VentaReal.Value);
            var porcentajeQuincena = metaQuincena > 0
                ? (ventaQuincena / metaQuincena) * 100
                : 0;

            hoja.Cell(fila, 1).Value = $"Q{q}";
            hoja.Cell(fila, 2).Value = metaQuincena;
            hoja.Cell(fila, 3).Value = ventaQuincena;
            hoja.Cell(fila, 4).Value = porcentajeQuincena;

            hoja.Cell(fila, 2).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
            hoja.Cell(fila, 3).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
            hoja.Cell(fila, 4).Style.NumberFormat.Format = "0.00%";

            fila++;
        }

        // Resumen por semanas
        fila += 2;
        hoja.Cell(fila, 1).Value = "RESUMEN POR SEMANAS";
        hoja.Cell(fila, 1).Style.Font.SetBold();
        hoja.Cell(fila, 1).Style.Font.FontSize = 14;
        fila++;

        hoja.Cell(fila, 1).Value = "Semana";
        hoja.Cell(fila, 2).Value = "Meta";
        hoja.Cell(fila, 3).Value = "Venta Real";
        hoja.Cell(fila, 4).Value = "Cumplimiento %";
        var semanaHeaderRange = hoja.Range(fila, 1, fila, 4);
        semanaHeaderRange.Style.Font.SetBold();
        semanaHeaderRange.Style.Fill.BackgroundColor = XLColor.FromArgb(200, 200, 200);

        fila++;
        var semanas = detallesOrdenados
            .GroupBy(d => GetNumeroSemanaFromDateOnly(d.Fecha))
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var semana in semanas)
        {
            var metaSemana = semana.Sum(d => d.MetaDiaria);
            var ventaSemana = semana
                .Where(d => d.VentaReal.HasValue)
                .Sum(d => d.VentaReal.Value);
            var porcentajeSemana = metaSemana > 0
                ? (ventaSemana / metaSemana) * 100
                : 0;

            hoja.Cell(fila, 1).Value = $"S{semana.Key}";
            hoja.Cell(fila, 2).Value = metaSemana;
            hoja.Cell(fila, 3).Value = ventaSemana;
            hoja.Cell(fila, 4).Value = porcentajeSemana;

            hoja.Cell(fila, 2).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
            hoja.Cell(fila, 3).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
            hoja.Cell(fila, 4).Style.NumberFormat.Format = "0.00%";

            fila++;
        }

        hoja.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var nombreArchivo = $"MetaMensual_{meta.IdAlmacenNavigation.Nombre.Replace(" ", "_")}_{meta.Mes}_{meta.Anio}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nombreArchivo);
    }

    private async Task<decimal> CalcularMetaMensual(
        int idAlmacen,
        int anio,
        int mes,
        int mesesHistoricos = 3)
    {
        var fechaInicio = new DateTime(anio, mes, 1);
        var fechaFin = fechaInicio.AddMonths(1);
        
        // 1. Obtener ventas del mismo mes del año anterior (para estacionalidad)
        var mesAnioAnterior = fechaInicio.AddYears(-1);
        var fechaInicioAnioAnterior = new DateTime(mesAnioAnterior.Year, mesAnioAnterior.Month, 1);
        var fechaFinAnioAnterior = fechaInicioAnioAnterior.AddMonths(1);

        var ventasMesAnioAnterior = await ObtenerVentasValidas(
            idAlmacen,
            fechaInicioAnioAnterior,
            fechaFinAnioAnterior);

        // 2. Calcular Factor de Crecimiento: Comparar últimos 3 meses de este año vs mismos 3 meses del año pasado
        var fechaActual = DateTime.Now;
        var mesActual = fechaActual.Month;
        var anioActual = fechaActual.Year;

        // Determinar el mes de referencia (último mes con datos disponibles)
        var mesReferencia = mesActual;
        var anioReferencia = anioActual;
        
        // Si estamos calculando para un mes futuro, usar el mes anterior como referencia
        if (anio > anioActual || (anio == anioActual && mes > mesActual))
        {
            // Retroceder al mes anterior
            var fechaRef = new DateTime(anioActual, mesActual, 1).AddMonths(-1);
            mesReferencia = fechaRef.Month;
            anioReferencia = fechaRef.Year;
        }

        // Últimos 3 meses de este año (hasta el mes de referencia, excluyendo el mes que estamos calculando)
        var mesesParaComparar = new List<(int Mes, int Anio)>();
        var fechaRefInicio = new DateTime(anioReferencia, mesReferencia, 1);
        
        for (int i = 1; i <= 3; i++)
        {
            var fechaMes = fechaRefInicio.AddMonths(-i);
            // No incluir el mes que estamos calculando
            if (!(fechaMes.Year == anio && fechaMes.Month == mes))
            {
                mesesParaComparar.Add((fechaMes.Month, fechaMes.Year));
            }
        }

        // Si no tenemos 3 meses válidos, usar los que tengamos
        if (mesesParaComparar.Count < 3)
        {
            mesesParaComparar.Clear();
            for (int i = 1; i <= 3; i++)
            {
                var fechaMes = fechaRefInicio.AddMonths(-i);
                mesesParaComparar.Add((fechaMes.Month, fechaMes.Year));
            }
        }

        // Obtener ventas de los últimos 3 meses de este año
        decimal ventasUltimos3MesesEsteAnio = 0;
        int mesesConDatosEsteAnio = 0;
        foreach (var (m, a) in mesesParaComparar)
        {
            var inicio = new DateTime(a, m, 1);
            var fin = inicio.AddMonths(1);
            var ventas = await ObtenerVentasValidas(idAlmacen, inicio, fin);
            if (ventas > 0)
            {
                ventasUltimos3MesesEsteAnio += ventas;
                mesesConDatosEsteAnio++;
            }
        }

        // Obtener ventas de los mismos 3 meses del año pasado
        decimal ventasMismos3MesesAnioPasado = 0;
        int mesesConDatosAnioPasado = 0;
        foreach (var (m, a) in mesesParaComparar)
        {
            var inicio = new DateTime(a - 1, m, 1);
            var fin = inicio.AddMonths(1);
            var ventas = await ObtenerVentasValidas(idAlmacen, inicio, fin);
            if (ventas > 0)
            {
                ventasMismos3MesesAnioPasado += ventas;
                mesesConDatosAnioPasado++;
            }
        }

        // 3. Calcular Factor de Crecimiento
        decimal factorCrecimiento = 1.0m; // Factor por defecto (sin crecimiento)
        
        if (ventasMismos3MesesAnioPasado > 0 && mesesConDatosAnioPasado > 0)
        {
            // Calcular promedio mensual para comparar mejor
            var promedioMensualEsteAnio = mesesConDatosEsteAnio > 0 
                ? ventasUltimos3MesesEsteAnio / mesesConDatosEsteAnio 
                : 0;
            var promedioMensualAnioPasado = ventasMismos3MesesAnioPasado / mesesConDatosAnioPasado;
            
            if (promedioMensualAnioPasado > 0)
            {
                factorCrecimiento = promedioMensualEsteAnio / promedioMensualAnioPasado;
                
                // Limitar el factor de crecimiento a un rango razonable (entre 0.5 y 2.0)
                // Esto evita factores extremos que podrían ser errores de datos
                if (factorCrecimiento < 0.5m)
                    factorCrecimiento = 0.5m;
                else if (factorCrecimiento > 2.0m)
                    factorCrecimiento = 2.0m;
            }
        }
        else if (ventasUltimos3MesesEsteAnio > 0 && mesesConDatosEsteAnio > 0)
        {
            // Si no hay datos del año pasado pero sí del año actual, usar un factor conservador
            factorCrecimiento = 1.1m; // 10% de crecimiento estimado
        }

        // 4. Calcular Meta Mensual: Mismo Mes Año Anterior * Factor de Crecimiento
        decimal metaMensual = 0;
        if (ventasMesAnioAnterior > 0)
        {
            // Aplicar factor de crecimiento al mismo mes del año anterior
            metaMensual = ventasMesAnioAnterior * factorCrecimiento;
        }
        else
        {
            // Si no hay datos del mismo mes del año anterior, usar promedio de los últimos 3 meses con factor
            if (ventasUltimos3MesesEsteAnio > 0 && mesesConDatosEsteAnio > 0)
            {
                var promedioMensual = ventasUltimos3MesesEsteAnio / mesesConDatosEsteAnio;
                metaMensual = promedioMensual * factorCrecimiento;
            }
            else if (ventasUltimos3MesesEsteAnio > 0)
            {
                // Fallback: usar promedio simple si no hay suficientes datos
                var promedioMensual = ventasUltimos3MesesEsteAnio / 3;
                metaMensual = promedioMensual;
            }
        }

        return Math.Round(metaMensual, 2);
    }

    private async Task<decimal> ObtenerVentasValidas(
        int idAlmacen,
        DateTime fechaInicio,
        DateTime fechaFin)
    {
        // Obtener ventas filtrando anuladas
        var ventasQuery = _context.VCierreVentaDiaria1s
            .AsNoTracking()
            .Where(v => v.IdAlmacen == idAlmacen &&
                        v.Fecha >= fechaInicio &&
                        v.Fecha < fechaFin);

        ventasQuery = from v in ventasQuery
                      join c in _context.ComprobanteDeVentaCabeceras
                          on new { v.IdAlmacen, v.Serie, v.Numero }
                          equals new { c.IdAlmacen, c.Serie, c.Numero }
                      where !(c.Estado == "A" &&
                              (string.IsNullOrEmpty(c.GeneroNc) || c.ExcluirDeEstadisticas) &&
                              c.FechaHoraUsuarioAnula.HasValue &&
                              c.FechaHoraUsuarioAnula.Value >= fechaInicio &&
                              c.FechaHoraUsuarioAnula.Value < fechaFin)
                      select v;

        var ventas = await ventasQuery
            .Select(v => new
            {
                v.Fecha,
                Descripcion = v.Descripcion,
                Soles = v.Soles,
                Vuelto = v.Vuelto
            })
            .ToListAsync();

        // Procesar ventas válidas
        decimal total = 0;
        foreach (var venta in ventas)
        {
            var tipo = (venta.Descripcion ?? string.Empty).Split(' ')[0];
            if (tipo != "N.C.")
            {
                var monto = tipo == "Efectivo"
                    ? venta.Soles - (venta.Vuelto ?? 0)
                    : venta.Soles;
                total += monto;
            }
        }

        return total;
    }

    private async Task<List<MetaMensualDetalle>> CalcularDistribucionDiaria(
        int idAlmacen,
        int anio,
        int mes,
        decimal metaMensual,
        int mesesHistoricos = 3)
    {
        var fechaInicio = new DateTime(anio, mes, 1);
        var diasEnMes = DateTime.DaysInMonth(anio, mes);

        // Obtener ventas históricas de los últimos N meses
        var fechaInicioHistorico = fechaInicio.AddMonths(-mesesHistoricos);
        var fechaFinHistorico = fechaInicio.AddDays(-1);

        // Obtener ventas históricas filtrando anuladas
        var ventasQuery = _context.VCierreVentaDiaria1s
            .AsNoTracking()
            .Where(v => v.IdAlmacen == idAlmacen &&
                        v.Fecha >= fechaInicioHistorico &&
                        v.Fecha < fechaInicio);

        ventasQuery = from v in ventasQuery
                      join c in _context.ComprobanteDeVentaCabeceras
                          on new { v.IdAlmacen, v.Serie, v.Numero }
                          equals new { c.IdAlmacen, c.Serie, c.Numero }
                      where !(c.Estado == "A" &&
                              (string.IsNullOrEmpty(c.GeneroNc) || c.ExcluirDeEstadisticas) &&
                              c.FechaHoraUsuarioAnula.HasValue &&
                              c.FechaHoraUsuarioAnula.Value >= fechaInicioHistorico &&
                              c.FechaHoraUsuarioAnula.Value < fechaInicio)
                      select v;

        var ventasHistoricas = await ventasQuery
            .Select(v => new
            {
                v.Fecha,
                DiaSemana = (int)v.Fecha.DayOfWeek, // 0=Domingo, 6=Sábado
                Descripcion = v.Descripcion,
                Soles = v.Soles,
                Vuelto = v.Vuelto
            })
            .ToListAsync();

        // Procesar ventas válidas y agrupar por fecha para obtener totales diarios
        var ventasPorFecha = new Dictionary<DateTime, decimal>();
        foreach (var venta in ventasHistoricas)
        {
            var tipo = (venta.Descripcion ?? string.Empty).Split(' ')[0];
            if (tipo != "N.C.")
            {
                var total = tipo == "Efectivo"
                    ? venta.Soles - (venta.Vuelto ?? 0)
                    : venta.Soles;
                
                // Agrupar por fecha (solo fecha, sin hora) para sumar totales diarios
                var fecha = venta.Fecha.Date;
                if (!ventasPorFecha.ContainsKey(fecha))
                {
                    ventasPorFecha[fecha] = 0;
                }
                ventasPorFecha[fecha] += total;
            }
        }

        // Calcular promedio de totales diarios por día de semana
        // Primero agrupar por día de semana, luego promediar los totales diarios
        var totalesDiariosPorDiaSemana = ventasPorFecha
            .GroupBy(kvp => ((int)kvp.Key.DayOfWeek)) // 0=Domingo, 6=Sábado
            .ToDictionary(
                g => g.Key,
                g => g.Select(kvp => (double)kvp.Value).ToList() // Lista de totales diarios para este día de semana
            );

        // Calcular el promedio de totales diarios por día de semana
        var promediosPorDiaSemana = totalesDiariosPorDiaSemana
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count > 0 ? kvp.Value.Average() : 0.0
            );

        // Si no hay datos históricos, distribución uniforme
        if (!promediosPorDiaSemana.Any())
        {
            var metaDiaria = metaMensual / diasEnMes;
            return Enumerable.Range(1, diasEnMes)
                .Select(dia =>
                {
                    var fecha = new DateTime(anio, mes, dia);
                    return new MetaMensualDetalle
                    {
                        Fecha = DateOnly.FromDateTime(fecha),
                        MetaDiaria = Math.Round(metaDiaria, 2)
                    };
                })
                .ToList();
        }

        // Si hay días de semana sin datos históricos, usar el promedio general como fallback
        var promedioGeneral = promediosPorDiaSemana.Values.Any() 
            ? promediosPorDiaSemana.Values.Average() 
            : 0.0;

        // Calcular total de pesos históricos
        var totalPesos = 0.0;
        var pesosPorDia = new Dictionary<int, double>();

        for (int dia = 1; dia <= diasEnMes; dia++)
        {
            var fecha = new DateTime(anio, mes, dia);
            var diaSemana = (int)fecha.DayOfWeek;
            // Si no hay datos para este día de semana, usar el promedio general
            var peso = promediosPorDiaSemana.GetValueOrDefault(diaSemana, promedioGeneral);
            pesosPorDia[dia] = peso;
            totalPesos += peso;
        }

        // Distribuir la meta mensual proporcionalmente
        var detalles = new List<MetaMensualDetalle>();
        for (int dia = 1; dia <= diasEnMes; dia++)
        {
            var fecha = new DateTime(anio, mes, dia);
            var peso = pesosPorDia[dia];
            var metaDiaria = totalPesos > 0
                ? (decimal)((peso / totalPesos) * (double)metaMensual)
                : metaMensual / diasEnMes;

            detalles.Add(new MetaMensualDetalle
            {
                Fecha = DateOnly.FromDateTime(fecha),
                MetaDiaria = Math.Round(metaDiaria, 2)
            });
        }

        return detalles;
    }

    private async Task<Dictionary<DateOnly, int>> ActualizarVentasReales(MetaMensual meta)
    {
        var fechaInicio = new DateTime(meta.Anio, meta.Mes, 1);
        var fechaFin = fechaInicio.AddMonths(1);

        // Obtener ventas filtrando anuladas
        var ventasQuery = _context.VCierreVentaDiaria1s
            .AsNoTracking()
            .Where(v => v.IdAlmacen == meta.IdAlmacen &&
                        v.Fecha >= fechaInicio &&
                        v.Fecha < fechaFin);

        ventasQuery = from v in ventasQuery
                      join c in _context.ComprobanteDeVentaCabeceras
                          on new { v.IdAlmacen, v.Serie, v.Numero }
                          equals new { c.IdAlmacen, c.Serie, c.Numero }
                      where !(c.Estado == "A" &&
                              (string.IsNullOrEmpty(c.GeneroNc) || c.ExcluirDeEstadisticas) &&
                              c.FechaHoraUsuarioAnula.HasValue &&
                              c.FechaHoraUsuarioAnula.Value >= fechaInicio &&
                              c.FechaHoraUsuarioAnula.Value < fechaFin)
                      select v;

        var ventas = await ventasQuery.ToListAsync();

        // Procesar ventas válidas
        var ventasValidas = new List<(DateOnly Fecha, decimal Total, string Serie, int Numero)>();
        foreach (var venta in ventas)
        {
            var fechaVenta = DateOnly.FromDateTime(venta.Fecha);
            var tipo = (venta.Descripcion ?? string.Empty).Split(' ')[0];
            if (tipo != "N.C.")
            {
                var total = tipo == "Efectivo"
                    ? venta.Soles - (venta.Vuelto ?? 0)
                    : venta.Soles;
                ventasValidas.Add((fechaVenta, total, venta.Serie, venta.Numero));
            }
        }

        var ventasPorFecha = ventasValidas
            .GroupBy(v => v.Fecha)
            .ToDictionary(g => g.Key, g => g.Sum(v => v.Total));

        // Contar clientes únicos por fecha (comprobantes únicos Serie+Numero)
        var clientesPorFecha = ventasValidas
            .GroupBy(v => v.Fecha)
            .ToDictionary(
                g => g.Key,
                g => g.Select(v => (v.Serie, v.Numero)).Distinct().Count()
            );

        foreach (var detalle in meta.MetaMensualDetalles)
        {
            if (ventasPorFecha.TryGetValue(detalle.Fecha, out var ventaReal))
            {
                detalle.VentaReal = Math.Round(ventaReal, 0);
                // Limitar porcentaje a 999.99 para evitar overflow en la BD (decimal(5,2))
                var porcentaje = detalle.MetaDiaria > 0
                    ? Math.Round((ventaReal / detalle.MetaDiaria) * 100, 1)
                    : 0;
                detalle.PorcentajeCumplimiento = Math.Min(porcentaje, 999.99m);
            }
            else
            {
                detalle.VentaReal = null;
                detalle.PorcentajeCumplimiento = null;
            }
        }


        await _context.SaveChangesAsync();
        
        return clientesPorFecha;
    }

    private static int GetNumeroSemana(DateTime fecha)
    {
        var inicioMes = new DateTime(fecha.Year, fecha.Month, 1);
        var diasTranscurridos = fecha.Day - 1;
        return (diasTranscurridos / 7) + 1;
    }

    private static int GetNumeroSemanaFromDateOnly(DateOnly fecha)
    {
        var diasTranscurridos = fecha.Day - 1;
        return (diasTranscurridos / 7) + 1;
    }

    private static bool IncluirEnReporteMetasMensuales(string? nombreAlmacen)
    {
        if (string.IsNullOrWhiteSpace(nombreAlmacen))
            return false;

        var nombre = nombreAlmacen.Trim();
        if (string.Equals(nombre, "PRINCIPAL", StringComparison.OrdinalIgnoreCase))
            return false;

        return !nombre.EndsWith('2');
    }
}

