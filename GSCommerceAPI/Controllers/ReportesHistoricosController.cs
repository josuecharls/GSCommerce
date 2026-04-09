using GSCommerce.Client.Models.DTOs.Reportes;
using GSCommerceAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportesHistoricosController : ControllerBase
    {
        private readonly SyscharlesContext _contextActual;
        private readonly SysDbHistoricalContext _contextHistorico;

        public ReportesHistoricosController(
            SyscharlesContext contextActual,
            SysDbHistoricalContext contextHistorico)
        {
            _contextActual = contextActual;
            _contextHistorico = contextHistorico;
        }

        [HttpGet("ventas-tarjeta-online-mensual")]
        [Authorize]
        public async Task<IActionResult> VentasTarjetaOnlineMensual(
            [FromQuery] int? anio,
            [FromQuery] string? nombreAlmacen)
        {
            var year = anio ?? DateTime.Today.Year;
            var reporte = await ObtenerReporteVentasTarjetaAnual(year, nombreAlmacen);
            return Ok(reporte);
        }

        [HttpGet("exportar-ventas-tarjeta-online-mensual")]
        [Authorize]
        public async Task<IActionResult> ExportarVentasTarjetaOnlineMensual(
            [FromQuery] int? anio,
            [FromQuery] string? nombreAlmacen)
        {
            var year = anio ?? DateTime.Today.Year;
            var reporte = await ObtenerReporteVentasTarjetaAnual(year, nombreAlmacen);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Ventas Tarjeta y Online");

            int fila = 1;
            worksheet.Cell(fila, 1).Value = "MES";
            worksheet.Cell(fila, 2).Value = "MÉTODO";
            worksheet.Cell(fila, 3).Value = "TIPO";
            worksheet.Cell(fila, 4).Value = "OPERACIONES";
            worksheet.Cell(fila, 5).Value = "SOLES (S/)";
            worksheet.Cell(fila, 6).Value = "DÓLARES ($)";
            worksheet.Cell(fila, 7).Value = "TOTAL";
            var rangeHeader = worksheet.Range(fila, 1, fila, 7);
            rangeHeader.Style.Font.Bold = true;
            rangeHeader.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            rangeHeader.Style.Font.FontColor = XLColor.White;
            rangeHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            fila++;

            foreach (var mesDto in reporte.Meses.OrderBy(m => m.Month))
            {
                var nombreMes = GetNombreMes(mesDto.Month);
                if (mesDto.Totales.Count == 0)
                {
                    worksheet.Cell(fila, 1).Value = nombreMes;
                    worksheet.Cell(fila, 2).Value = "(sin operaciones)";
                    fila++;
                    continue;
                }

                foreach (var t in mesDto.Totales)
                {
                    worksheet.Cell(fila, 1).Value = nombreMes;
                    worksheet.Cell(fila, 2).Value = t.Descripcion;
                    worksheet.Cell(fila, 3).Value = t.Tipo;
                    worksheet.Cell(fila, 4).Value = t.CantidadOperaciones;
                    worksheet.Cell(fila, 5).Value = t.TotalSoles;
                    worksheet.Cell(fila, 5).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(fila, 6).Value = t.TotalDolares;
                    worksheet.Cell(fila, 6).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(fila, 7).Value = t.Total;
                    worksheet.Cell(fila, 7).Style.NumberFormat.Format = "#,##0.00";
                    fila++;
                }
            }

            fila++;
            worksheet.Cell(fila, 1).Value = "TOTAL AÑO";
            worksheet.Cell(fila, 1).Style.Font.Bold = true;
            fila++;

            foreach (var t in reporte.TotalesAnio)
            {
                worksheet.Cell(fila, 2).Value = t.Descripcion;
                worksheet.Cell(fila, 3).Value = t.Tipo;
                worksheet.Cell(fila, 4).Value = t.CantidadOperaciones;
                worksheet.Cell(fila, 5).Value = t.TotalSoles;
                worksheet.Cell(fila, 5).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(fila, 6).Value = t.TotalDolares;
                worksheet.Cell(fila, 6).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(fila, 7).Value = t.Total;
                worksheet.Cell(fila, 7).Style.NumberFormat.Format = "#,##0.00";
                fila++;
            }

            if (reporte.TotalesAnio.Any())
            {
                var ts = reporte.TotalesAnio.Sum(x => x.TotalSoles);
                var td = reporte.TotalesAnio.Sum(x => x.TotalDolares);
                var op = reporte.TotalesAnio.Sum(x => x.CantidadOperaciones);
                worksheet.Cell(fila, 1).Value = "TOTAL";
                worksheet.Cell(fila, 1).Style.Font.Bold = true;
                worksheet.Cell(fila, 4).Value = op;
                worksheet.Cell(fila, 4).Style.Font.Bold = true;
                worksheet.Cell(fila, 5).Value = ts;
                worksheet.Cell(fila, 5).Style.Font.Bold = true;
                worksheet.Cell(fila, 5).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(fila, 6).Value = td;
                worksheet.Cell(fila, 6).Style.Font.Bold = true;
                worksheet.Cell(fila, 6).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(fila, 7).Value = ts + td;
                worksheet.Cell(fila, 7).Style.Font.Bold = true;
                worksheet.Cell(fila, 7).Style.NumberFormat.Format = "#,##0.00";
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ReporteVentasTarjetas_{year}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        private sealed class FilaPagoTarjetaAnual
        {
            public int Month { get; init; }
            public int IdTipoPagoVenta { get; init; }
            public string Tipo { get; init; } = string.Empty;
            public string Metodo { get; init; } = string.Empty;
            public decimal Soles { get; init; }
            public decimal Dolares { get; init; }
        }

        private static List<PagoTarjetaOnlineResumenDTO> AgruparPorMetodo(IEnumerable<FilaPagoTarjetaAnual> filas)
        {
            return filas
                .GroupBy(x => new { x.IdTipoPagoVenta, x.Tipo, x.Metodo })
                .Select(g => new PagoTarjetaOnlineResumenDTO
                {
                    IdTipoPagoVenta = g.Key.IdTipoPagoVenta,
                    Tipo = g.Key.Tipo,
                    Descripcion = g.Key.Metodo,
                    TotalSoles = g.Sum(x => x.Soles),
                    TotalDolares = g.Sum(x => x.Dolares),
                    CantidadOperaciones = g.Count()
                })
                .OrderBy(t => t.Tipo)
                .ThenBy(t => t.Descripcion)
                .ToList();
        }

        private async Task<ReporteVentasTarjetaMensualResponseDTO> ObtenerReporteVentasTarjetaAnual(int anio, string? nombreAlmacen)
        {
            if (anio < 2000 || anio > 2100)
                anio = DateTime.Today.Year;

            var fechaInicio = new DateTime(anio, 1, 1);
            var fechaFinExclusiva = new DateTime(anio, 12, 31).AddDays(1);
            var fechaCorte = new DateTime(2025, 8, 10);
            var filas = new List<FilaPagoTarjetaAnual>();

            if (fechaInicio < fechaCorte)
            {
                var finHistorico = fechaFinExclusiva < fechaCorte ? fechaFinExclusiva : fechaCorte;

                var queryHist = await (
                    from cabecera in _contextHistorico.ComprobanteDeVentaCabeceras.AsNoTracking()
                    join pago in _contextHistorico.VDetallePagoVenta1s.AsNoTracking()
                        on cabecera.IdComprobante equals pago.IdComprobante
                    join tipo in _contextHistorico.TipoPagoVenta.AsNoTracking()
                        on pago.IdTipoPagoVenta equals tipo.IdTipoPagoVenta
                    join almacen in _contextHistorico.Almacens.AsNoTracking()
                        on cabecera.IdAlmacen equals almacen.IdAlmacen
                    where cabecera.Fecha >= fechaInicio
                          && cabecera.Fecha < finHistorico
                          && almacen.Nombre != null
                          && (almacen.Nombre == "CHICLAYO2" || almacen.Nombre == "PIURA2" || almacen.Nombre == "SULLANA2" || almacen.Nombre == "TRUJILLO2" || almacen.Nombre == "AGUAS VERDES2" || almacen.Nombre == "IQUITOS FATISHOP")
                          && (string.IsNullOrEmpty(nombreAlmacen) || almacen.Nombre == nombreAlmacen)
                          && !(cabecera.Estado == "A" && cabecera.GeneroNc == null)
                          && (tipo.Tipo == "Tarjeta" || tipo.Tipo == "Online")
                    select new
                    {
                        cabecera.Fecha.Month,
                        tipo.IdTipoPagoVenta,
                        tipo.Tipo,
                        Metodo = tipo.Descripcion,
                        pago.Soles,
                        pago.Dolares
                    }).ToListAsync();

                // SGV: el sistema antiguo mezclaba/mal duplicaba soles y dólares; solo usamos soles.
                filas.AddRange(queryHist.Select(x => new FilaPagoTarjetaAnual
                {
                    Month = x.Month,
                    IdTipoPagoVenta = x.IdTipoPagoVenta,
                    Tipo = x.Tipo,
                    Metodo = x.Metodo,
                    Soles = x.Soles,
                    Dolares = 0m
                }));
            }

            if (fechaFinExclusiva > fechaCorte)
            {
                var inicioActual = fechaInicio > fechaCorte ? fechaInicio : fechaCorte;

                var queryActual = await (
                    from cabecera in _contextActual.ComprobanteDeVentaCabeceras.AsNoTracking()
                    join pago in _contextActual.VDetallePagoVenta1s.AsNoTracking()
                        on cabecera.IdComprobante equals pago.IdComprobante
                    join tipo in _contextActual.TipoPagoVenta.AsNoTracking()
                        on pago.IdTipoPagoVenta equals tipo.IdTipoPagoVenta
                    join almacen in _contextActual.Almacens.AsNoTracking()
                        on cabecera.IdAlmacen equals almacen.IdAlmacen
                    where cabecera.Fecha >= inicioActual
                          && cabecera.Fecha < fechaFinExclusiva
                          && almacen.Nombre != null
                          && (almacen.Nombre == "CHICLAYO2" || almacen.Nombre == "PIURA2" || almacen.Nombre == "SULLANA2" || almacen.Nombre == "TRUJILLO2" || almacen.Nombre == "AGUAS VERDES2" || almacen.Nombre == "IQUITOS FATISHOP")
                          && (string.IsNullOrEmpty(nombreAlmacen) || almacen.Nombre == nombreAlmacen)
                          && !(cabecera.Estado == "A" && (cabecera.GeneroNc == null || cabecera.ExcluirDeEstadisticas))
                          && (tipo.Tipo == "Tarjeta" || tipo.Tipo == "Online")
                    select new
                    {
                        cabecera.Fecha.Month,
                        tipo.IdTipoPagoVenta,
                        tipo.Tipo,
                        Metodo = tipo.Descripcion,
                        pago.Soles,
                        pago.Dolares
                    }).ToListAsync();

                filas.AddRange(queryActual.Select(x => new FilaPagoTarjetaAnual
                {
                    Month = x.Month,
                    IdTipoPagoVenta = x.IdTipoPagoVenta,
                    Tipo = x.Tipo,
                    Metodo = x.Metodo,
                    Soles = x.Soles,
                    Dolares = x.Dolares
                }));
            }

            var meses = Enumerable.Range(1, 12)
                .Select(m => new VentaTarjetaMesTotalesDTO
                {
                    Month = m,
                    Totales = AgruparPorMetodo(filas.Where(f => f.Month == m))
                })
                .ToList();

            return new ReporteVentasTarjetaMensualResponseDTO
            {
                Year = anio,
                Meses = meses,
                TotalesAnio = AgruparPorMetodo(filas)
            };
        }

        [HttpPost("ventas-familias")]
        [Authorize]
        public async Task<IActionResult> VentasFamilias([FromBody] VentasFamiliaRequestDTO request)
        {
            if (request == null || !request.Anios.Any() || !request.Meses.Any())
                return BadRequest("Debe seleccionar al menos un año y un mes.");

            var fechaCorte = new DateTime(2025, 8, 10);
            var resultadosComb = new List<VentaFamiliaMensualDTO>();

            // Optimize multi-select filter conditions by using memory structures
            var familiasHash = request.Familias.ToHashSet();
            var marcasHash = request.Marcas.ToHashSet();
            var aniosHash = request.Anios.ToHashSet();
            var mesesHash = request.Meses.ToHashSet();

            bool requiereHistorico = request.Anios.Any(a => a <= 2025);
            bool requiereActual = request.Anios.Any(a => a >= 2025);

            if (requiereHistorico)
            {
                var queryHist = await (
                    from cabecera in _contextHistorico.ComprobanteDeVentaCabeceras.AsNoTracking()
                    join detalle in _contextHistorico.ComprobanteDeVentaDetalles.AsNoTracking()
                        on cabecera.IdComprobante equals detalle.IdComprobante
                    join articulo in _contextHistorico.Articulos.AsNoTracking()
                        on detalle.IdArticulo equals articulo.IdArticulo
                    join almacen in _contextHistorico.Almacens.AsNoTracking()
                        on cabecera.IdAlmacen equals almacen.IdAlmacen
                    where cabecera.Fecha < fechaCorte
                          && aniosHash.Contains(cabecera.Fecha.Year)
                          && mesesHash.Contains(cabecera.Fecha.Month)
                          && almacen.Nombre != null
                          && almacen.Nombre != "PRINCIPAL"
                          && !(cabecera.Estado == "A" && cabecera.GeneroNc == null)
                          && (familiasHash.Count == 0 || familiasHash.Contains(articulo.Familia))
                          && (marcasHash.Count == 0 || marcasHash.Contains(articulo.Marca))
                    select new
                    {
                        cabecera.Fecha.Year,
                        cabecera.Fecha.Month,
                        AlmacenNombre = almacen.Nombre,
                        detalle.Cantidad
                    }).ToListAsync();

                var groupedHist = queryHist
                    .GroupBy(x => new { x.Year, x.Month, x.AlmacenNombre })
                    .Select(g => new VentaFamiliaMensualDTO
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        AlmacenNombre = g.Key.AlmacenNombre,
                        CantidadVendida = g.Sum(x => x.Cantidad)
                    });

                resultadosComb.AddRange(groupedHist);
            }

            if (requiereActual)
            {
                var queryActual = await (
                    from cabecera in _contextActual.ComprobanteDeVentaCabeceras.AsNoTracking()
                    join detalle in _contextActual.ComprobanteDeVentaDetalles.AsNoTracking()
                        on cabecera.IdComprobante equals detalle.IdComprobante
                    join articulo in _contextActual.Articulos.AsNoTracking()
                        on detalle.IdArticulo equals articulo.IdArticulo
                    join almacen in _contextActual.Almacens.AsNoTracking()
                        on cabecera.IdAlmacen equals almacen.IdAlmacen
                    where cabecera.Fecha >= fechaCorte
                          && aniosHash.Contains(cabecera.Fecha.Year)
                          && mesesHash.Contains(cabecera.Fecha.Month)
                          && almacen.Nombre != null
                          && almacen.Nombre != "PRINCIPAL"
                          && !(cabecera.Estado == "A" && (cabecera.GeneroNc == null || cabecera.ExcluirDeEstadisticas))
                          && (familiasHash.Count == 0 || familiasHash.Contains(articulo.Familia))
                          && (marcasHash.Count == 0 || marcasHash.Contains(articulo.Marca))
                    select new
                    {
                        cabecera.Fecha.Year,
                        cabecera.Fecha.Month,
                        AlmacenNombre = almacen.Nombre,
                        detalle.Cantidad
                    }).ToListAsync();

                var groupedActual = queryActual
                    .GroupBy(x => new { x.Year, x.Month, x.AlmacenNombre })
                    .Select(g => new VentaFamiliaMensualDTO
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        AlmacenNombre = g.Key.AlmacenNombre,
                        CantidadVendida = g.Sum(x => x.Cantidad)
                    });

                resultadosComb.AddRange(groupedActual);
            }

            var consolidado = resultadosComb
                .GroupBy(x => new { x.Year, x.Month, x.AlmacenNombre })
                .Select(g => new VentaFamiliaMensualDTO
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    AlmacenNombre = g.Key.AlmacenNombre,
                    CantidadVendida = g.Sum(x => x.CantidadVendida)
                })
                .ToList();

            return Ok(new ReporteVentasFamiliaResponseDTO { Resultados = consolidado });
        }

        private string GetNombreMes(int mes)
        {
            return mes switch
            {
                1 => "Enero",
                2 => "Febrero",
                3 => "Marzo",
                4 => "Abril",
                5 => "Mayo",
                6 => "Junio",
                7 => "Julio",
                8 => "Agosto",
                9 => "Septiembre",
                10 => "Octubre",
                11 => "Noviembre",
                12 => "Diciembre",
                _ => mes.ToString()
            };
        }
    }
}