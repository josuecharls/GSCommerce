using System.Globalization;
using System.IO;
using System.Text.Json;
using GSCommerce.Client.Models.DTOs.Reportes;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace GSCommerceAPI.Controllers;

[ApiController]
[Route("api/reportes/gastos")]
public class ReportesGastosController : ControllerBase
{
    private const string AlquilerKeyPrefix = "GASTOS_ALQUILER_";
    private const string DetalleKeyPrefix = "GASTOS_REPORTE_";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private static readonly Dictionary<string, string[]> TiposGasto = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Planillas"] = new[] { "PAGO PLANILLAS" },
        ["Agua"] = new[] { "PAGO AGUA" },
        ["Luz"] = new[] { "PAGO LUZ" },
        ["Internet"] = new[] { "PAGO INTERNET", "PAGO TELEFONO", "PAGO CELULAR" },
        ["Diversos"] = new[] { "GASTOS DIVERSOS" }
    };

    private readonly SyscharlesContext _context;

    public ReportesGastosController(SyscharlesContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReporteGastosInversionDTO>>> Obtener([FromQuery] int anio, [FromQuery] int mes, [FromQuery] int? idAlmacen)
    {
        if (mes is < 1 or > 12)
        {
            return BadRequest("Mes inválido.");
        }

        var datos = await ObtenerDatosAsync(anio, mes, idAlmacen);
        return datos;
    }

    [HttpPost("detalles")]
    public async Task<IActionResult> GuardarDetalles([FromBody] GuardarReporteGastosRequest request)
    {
        if (request is null)
        {
            return BadRequest("Datos inválidos.");
        }

        if (request.Mes is < 1 or > 12)
        {
            return BadRequest("Mes inválido.");
        }

        var key = ObtenerClaveDetalle(request.Anio, request.Mes, request.IdAlmacen);
        var payload = new ManualGastoDetalle
        {
            Planillas = request.Planillas,
            Agua = request.Agua,
            Luz = request.Luz,
            Internet = request.Internet,
            Sunat = request.Sunat,
            Diversos = request.Diversos,
            PlanillaServicios = request.PlanillaServicios
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var config = await _context.Configuracions.FirstOrDefaultAsync(c => c.Configuracion1 == key);

        if (config is null)
        {
            config = new Configuracion
            {
                Configuracion1 = key,
                Valor = json,
                Descripcion = $"Valores reporte gastos {request.Mes:D2}/{request.Anio} almacén {request.IdAlmacen}"
            };
            _context.Configuracions.Add(config);
        }
        else
        {
            config.Valor = json;
            config.Descripcion = $"Valores reporte gastos {request.Mes:D2}/{request.Anio} almacén {request.IdAlmacen}";
            _context.Configuracions.Update(config);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("alquiler")]
    public async Task<IActionResult> GuardarAlquiler([FromBody] GuardarReporteGastosAlquilerRequest request)
    {
        if (request is null)
        {
            return BadRequest("Datos inválidos.");
        }

        var key = ObtenerClaveAlquiler(request.IdAlmacen);
        var valor = request.Alquiler.ToString(CultureInfo.InvariantCulture);

        var config = await _context.Configuracions.FirstOrDefaultAsync(c => c.Configuracion1 == key);
        if (config is null)
        {
            config = new Configuracion
            {
                Configuracion1 = key,
                Valor = valor,
                Descripcion = $"Alquiler almacén {request.IdAlmacen}"
            };
            _context.Configuracions.Add(config);
        }
        else
        {
            config.Valor = valor;
            if (string.IsNullOrWhiteSpace(config.Descripcion))
            {
                config.Descripcion = $"Alquiler almacén {request.IdAlmacen}";
            }
            _context.Configuracions.Update(config);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar([FromQuery] int anio, [FromQuery] int mes, [FromQuery] int? idAlmacen)
    {
        if (mes is < 1 or > 12)
        {
            return BadRequest("Mes inválido.");
        }

        var datos = await ObtenerDatosAsync(anio, mes, idAlmacen);
        if (!datos.Any())
        {
            return NotFound("No se encontraron datos para el periodo indicado.");
        }

        var cultura = CultureInfo.CreateSpecificCulture("es-PE");
        var fecha = new DateTime(anio, mes, 1);
        var nombreMes = cultura.TextInfo.ToTitleCase(fecha.ToString("MMMM", cultura));

        using var workbook = new XLWorkbook();
        var hoja = workbook.Worksheets.Add("Gastos");

        var encabezado = $"INVERSIÓN MES {fecha.ToString("MMMM", cultura).ToUpper(cultura)}";
        hoja.Cell(1, 1).Value = encabezado;
        hoja.Range(1, 1, 1, 14).Merge();
        hoja.Cell(1, 1).Style.Font.SetBold();
        hoja.Cell(1, 1).Style.Font.FontSize = 16;
        hoja.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        hoja.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(230, 230, 230);

        var headers = new[]
        {
            "TIENDA",
            "ALQUILER",
            "PLANILLAS",
            "AGUA",
            "LUZ",
            "INTERNET",
            "SUNAT",
            "DIVERSOS",
            "PLANILLA BCP/ALQUILER - SERVICIOS",
            "TOTAL INVERSIÓN",
            $"VENTA {nombreMes.ToUpper(cultura)}",
            "COSTO DE MERCADERÍA",
            "UTILIDAD BRUTA",
            "UTILIDAD NETA"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = hoja.Cell(2, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.SetBold();
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(200, 200, 200);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        var fila = 3;
        foreach (var item in datos)
        {
            hoja.Cell(fila, 1).Value = item.Tienda;
            hoja.Cell(fila, 2).Value = item.Alquiler;
            hoja.Cell(fila, 3).Value = item.Planillas;
            hoja.Cell(fila, 4).Value = item.Agua;
            hoja.Cell(fila, 5).Value = item.Luz;
            hoja.Cell(fila, 6).Value = item.Internet;
            hoja.Cell(fila, 7).Value = item.Sunat;
            hoja.Cell(fila, 8).Value = item.Diversos;
            hoja.Cell(fila, 9).Value = item.PlanillaServicios;
            hoja.Cell(fila, 10).Value = item.TotalInversion;
            hoja.Cell(fila, 11).Value = item.Venta;
            hoja.Cell(fila, 12).Value = item.CostoMercaderia;
            hoja.Cell(fila, 13).Value = item.UtilidadBruta;
            hoja.Cell(fila, 14).Value = item.UtilidadNeta;
            fila++;
        }

        hoja.Range(3, 2, fila - 1, 14).Style.NumberFormat.Format = "S/ #,##0.00";

        hoja.Cell(fila, 1).Value = "TOTAL";
        hoja.Cell(fila, 1).Style.Font.SetBold();

        hoja.Cell(fila, 2).Value = datos.Sum(x => x.Alquiler);
        hoja.Cell(fila, 3).Value = datos.Sum(x => x.Planillas);
        hoja.Cell(fila, 4).Value = datos.Sum(x => x.Agua);
        hoja.Cell(fila, 5).Value = datos.Sum(x => x.Luz);
        hoja.Cell(fila, 6).Value = datos.Sum(x => x.Internet);
        hoja.Cell(fila, 7).Value = datos.Sum(x => x.Sunat);
        hoja.Cell(fila, 8).Value = datos.Sum(x => x.Diversos);
        hoja.Cell(fila, 9).Value = datos.Sum(x => x.PlanillaServicios);
        hoja.Cell(fila, 10).Value = datos.Sum(x => x.TotalInversion);
        hoja.Cell(fila, 11).Value = datos.Sum(x => x.Venta);
        hoja.Cell(fila, 12).Value = datos.Sum(x => x.CostoMercaderia);
        hoja.Cell(fila, 13).Value = datos.Sum(x => x.UtilidadBruta);
        hoja.Cell(fila, 14).Value = datos.Sum(x => x.UtilidadNeta);
        hoja.Range(fila, 2, fila, 14).Style.NumberFormat.Format = "S/ #,##0.00";
        hoja.Range(fila, 1, fila, 14).Style.Fill.BackgroundColor = XLColor.FromArgb(235, 241, 222);
        hoja.Range(fila, 1, fila, 14).Style.Font.SetBold();

        hoja.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var nombreArchivo = $"ReporteGastos_{fecha:yyyy_MM}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
    }

    private async Task<List<ReporteGastosInversionDTO>> ObtenerDatosAsync(int anio, int mes, int? idAlmacen)
    {
        var fechaInicio = new DateTime(anio, mes, 1);
        var fechaFin = fechaInicio.AddMonths(1);

        var almacenesQuery = _context.Almacens
            .AsNoTracking()
            .Where(a => a.EsTienda && a.Estado);

        if (idAlmacen.HasValue && idAlmacen.Value > 0)
        {
            almacenesQuery = almacenesQuery.Where(a => a.IdAlmacen == idAlmacen.Value);
        }

        var almacenes = await almacenesQuery
            .OrderBy(a => a.Nombre)
            .Select(a => new { a.IdAlmacen, a.Nombre })
            .ToListAsync();

        if (!almacenes.Any())
        {
            return new List<ReporteGastosInversionDTO>();
        }

        var ids = almacenes.Select(a => a.IdAlmacen).ToList();

        var alquilerKeys = ids.Select(ObtenerClaveAlquiler).ToList();
        var detalleKeys = ids.Select(id => ObtenerClaveDetalle(anio, mes, id)).ToList();

        var configuraciones = await _context.Configuracions
            .Where(c => alquilerKeys.Contains(c.Configuracion1) || detalleKeys.Contains(c.Configuracion1))
            .ToListAsync();

        var alquileres = configuraciones
            .Where(c => c.Configuracion1.StartsWith(AlquilerKeyPrefix, StringComparison.Ordinal))
            .Select(c => new { Config = c, Id = ExtraerIdAlmacen(c.Configuracion1, AlquilerKeyPrefix) })
            .Where(x => x.Id > 0 && ids.Contains(x.Id))
            .ToDictionary(x => x.Id, x => x.Config);

        var detalles = configuraciones
            .Where(c => c.Configuracion1.StartsWith(DetalleKeyPrefix, StringComparison.Ordinal))
            .ToDictionary(c => c.Configuracion1, c => c);

        var egresos = await _context.IngresosEgresosCabeceras
            .AsNoTracking()
            .Where(e => e.Naturaleza == "E" && e.Estado == "E")
            .Where(e => e.Fecha >= fechaInicio && e.Fecha < fechaFin)
            .Where(e => ids.Contains(e.IdAlmacen))
            .Select(e => new EgresoResumen
            {
                IdAlmacen = e.IdAlmacen,
                Tipo = e.Tipo,
                Monto = e.Monto
            })
            .ToListAsync();

        var egresosPorAlmacen = egresos
            .GroupBy(e => e.IdAlmacen)
            .ToDictionary(g => g.Key, g => g.ToList());

        var ventas = await ObtenerVentasAsync(ids, fechaInicio, fechaFin);
        var utilidades = await ObtenerUtilidadesAsync(ids, fechaInicio, fechaFin);

        var resultado = new List<ReporteGastosInversionDTO>();

        foreach (var almacen in almacenes)
        {
            var detalleKey = ObtenerClaveDetalle(anio, mes, almacen.IdAlmacen);
            ManualGastoDetalle? manual = null;
            if (detalles.TryGetValue(detalleKey, out var detalleConfig))
            {
                try
                {
                    manual = JsonSerializer.Deserialize<ManualGastoDetalle>(detalleConfig.Valor, JsonOptions);
                }
                catch
                {
                    manual = null;
                }
            }

            decimal ObtenerManual(Func<ManualGastoDetalle, decimal> selector, decimal calculado)
                => manual is null ? calculado : selector(manual);

            var egresosAlmacen = egresosPorAlmacen.TryGetValue(almacen.IdAlmacen, out var listaEgresos)
                ? listaEgresos
                : new List<EgresoResumen>();

            decimal planillasCalculadas = SumarEgresos(egresosAlmacen, TiposGasto["Planillas"]);
            decimal aguaCalculada = SumarEgresos(egresosAlmacen, TiposGasto["Agua"]);
            decimal luzCalculada = SumarEgresos(egresosAlmacen, TiposGasto["Luz"]);
            decimal internetCalculado = SumarEgresos(egresosAlmacen, TiposGasto["Internet"]);
            decimal diversosCalculado = SumarEgresos(egresosAlmacen, TiposGasto["Diversos"]);

            var alquiler = alquileres.TryGetValue(almacen.IdAlmacen, out var alquilerConfig)
                ? ParseDecimal(alquilerConfig.Valor)
                : 0m;

            var planillas = ObtenerManual(x => x.Planillas, planillasCalculadas);
            var agua = ObtenerManual(x => x.Agua, aguaCalculada);
            var luz = ObtenerManual(x => x.Luz, luzCalculada);
            var internet = ObtenerManual(x => x.Internet, internetCalculado);
            var sunat = ObtenerManual(x => x.Sunat, 0m);
            var diversos = ObtenerManual(x => x.Diversos, diversosCalculado);
            var planillaServicios = ObtenerManual(x => x.PlanillaServicios, 0m);

            var totalInversion = alquiler + planillas + agua + luz + internet + sunat + diversos + planillaServicios;

            var venta = ventas.TryGetValue(almacen.IdAlmacen, out var ventaValor) ? ventaValor : 0m;
            var utilidadBruta = utilidades.TryGetValue(almacen.IdAlmacen, out var utilidadValor) ? utilidadValor : 0m;
            var costoMercaderia = venta - utilidadBruta;
            var utilidadNeta = utilidadBruta - totalInversion;

            resultado.Add(new ReporteGastosInversionDTO
            {
                IdAlmacen = almacen.IdAlmacen,
                Tienda = almacen.Nombre,
                Alquiler = Math.Round(alquiler, 2),
                Planillas = Math.Round(planillas, 2),
                Agua = Math.Round(agua, 2),
                Luz = Math.Round(luz, 2),
                Internet = Math.Round(internet, 2),
                Sunat = Math.Round(sunat, 2),
                Diversos = Math.Round(diversos, 2),
                PlanillaServicios = Math.Round(planillaServicios, 2),
                TotalInversion = Math.Round(totalInversion, 2),
                Venta = Math.Round(venta, 2),
                CostoMercaderia = Math.Round(costoMercaderia, 2),
                UtilidadBruta = Math.Round(utilidadBruta, 2),
                UtilidadNeta = Math.Round(utilidadNeta, 2),
                TieneAlquilerFijo = alquilerConfig is not null
            });
        }

        return resultado;
    }

    private async Task<Dictionary<int, decimal>> ObtenerVentasAsync(List<int> ids, DateTime inicio, DateTime fin)
    {
        var ventasQuery = _context.VCierreVentaDiaria1s
            .AsNoTracking()
            .Where(v => v.Fecha >= inicio && v.Fecha < fin)
            .Where(v => ids.Contains(v.IdAlmacen));

        ventasQuery =
            from v in ventasQuery
            join c in _context.ComprobanteDeVentaCabeceras.AsNoTracking()
                on new { v.IdAlmacen, v.Serie, v.Numero } equals new { c.IdAlmacen, c.Serie, c.Numero }
            where !(c.Estado == "A" && string.IsNullOrEmpty(c.GeneroNc) && c.FechaHoraUsuarioAnula.HasValue &&
                    c.FechaHoraUsuarioAnula.Value.Date == v.Fecha.Date)
            select v;

        var ventas = await ventasQuery.ToListAsync();

        var resultado = new Dictionary<int, decimal>();
        foreach (var grupo in ventas.GroupBy(v => v.IdAlmacen))
        {
            decimal efectivo = 0m;
            decimal tarjeta = 0m;
            foreach (var item in grupo)
            {
                var descripcion = (item.Descripcion ?? string.Empty).Split(' ')[0];
                switch (descripcion)
                {
                    case "Efectivo":
                        efectivo += item.Soles - (item.Vuelto ?? 0m);
                        break;
                    case "Tarjeta":
                    case "Online":
                        tarjeta += item.Soles;
                        break;
                }
            }

            resultado[grupo.Key] = efectivo + tarjeta;
        }

        return resultado;
    }

    private async Task<Dictionary<int, decimal>> ObtenerUtilidadesAsync(List<int> ids, DateTime inicio, DateTime fin)
    {
        var utilidades = await (
            from d in _context.VDetallesVentas.AsNoTracking()
            where d.Fecha >= inicio && d.Fecha < fin && ids.Contains(d.IdAlmacen)
            let total = d.Total ?? 0m
            let costoBase = (d.Costo ?? 0m) != 0m
                ? (d.Costo ?? 0m)
                : d.PrecioCompra * (decimal)(d.Cantidad ?? 0)
            let costoAjustado = total >= 0m ? costoBase : -costoBase
            group new { total, costoAjustado } by d.IdAlmacen
            into g
            select new
            {
                IdAlmacen = g.Key,
                Venta = g.Sum(x => x.total),
                Costo = g.Sum(x => x.costoAjustado)
            })
            .ToListAsync();

        return utilidades.ToDictionary(x => x.IdAlmacen, x => x.Venta - x.Costo);
    }

    private static decimal SumarEgresos(List<EgresoResumen> egresos, string[] tipos)
    {
        if (egresos.Count == 0)
        {
            return 0m;
        }

        var set = new HashSet<string>(tipos, StringComparer.OrdinalIgnoreCase);
        return egresos
            .Where(e => set.Contains(e.Tipo))
            .Sum(e => e.Monto);
    }

    private static decimal ParseDecimal(string? valor)
    {
        if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.GetCultureInfo("es-PE"), out result))
        {
            return result;
        }

        return 0m;
    }

    private static string ObtenerClaveAlquiler(int idAlmacen) => $"{AlquilerKeyPrefix}{idAlmacen}";

    private static string ObtenerClaveDetalle(int anio, int mes, int idAlmacen) => $"{DetalleKeyPrefix}{anio}{mes:D2}_{idAlmacen}";

    private static int ExtraerIdAlmacen(string clave, string prefijo)
        => int.TryParse(clave[prefijo.Length..], out var valor) ? valor : 0;

    private class EgresoResumen
    {
        public int IdAlmacen { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }

    private class ManualGastoDetalle
    {
        public decimal Planillas { get; set; }
        public decimal Agua { get; set; }
        public decimal Luz { get; set; }
        public decimal Internet { get; set; }
        public decimal Sunat { get; set; }
        public decimal Diversos { get; set; }
        public decimal PlanillaServicios { get; set; }
    }
}