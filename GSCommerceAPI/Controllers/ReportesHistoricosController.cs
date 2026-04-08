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
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] string? nombreAlmacen)
        {
            var reporteGlobal = await ObtenerDatosReporte(desde, hasta, nombreAlmacen);
            return Ok(new ReporteVentasTarjetaMensualResponseDTO { Resultados = reporteGlobal });
        }

        [HttpGet("exportar-ventas-tarjeta-online-mensual")]
        [Authorize]
        public async Task<IActionResult> ExportarVentasTarjetaOnlineMensual(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] string? nombreAlmacen)
        {
            var reporteGlobal = await ObtenerDatosReporte(desde, hasta, nombreAlmacen);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Ventas Tarjeta y Online");

            // Cabeceras
            worksheet.Cell(1, 1).Value = "AÑO";
            worksheet.Cell(1, 2).Value = "MES";
            worksheet.Cell(1, 3).Value = "ALMACÉN";
            worksheet.Cell(1, 4).Value = "TOTAL SOLES (S/)";

            var rangeHeader = worksheet.Range("A1:D1");
            rangeHeader.Style.Font.Bold = true;
            rangeHeader.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            rangeHeader.Style.Font.FontColor = XLColor.White;
            rangeHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Datos
            int fila = 2;
            foreach (var item in reporteGlobal)
            {
                worksheet.Cell(fila, 1).Value = item.Year;
                worksheet.Cell(fila, 2).Value = GetNombreMes(item.Month);
                worksheet.Cell(fila, 3).Value = item.AlmacenNombre;
                worksheet.Cell(fila, 4).Value = item.TotalSoles;
                worksheet.Cell(fila, 4).Style.NumberFormat.Format = "#,##0.00";
                fila++;
            }

            // Totales
            if (reporteGlobal.Any())
            {
                worksheet.Cell(fila, 3).Value = "TOTAL GENERAL:";
                worksheet.Cell(fila, 3).Style.Font.Bold = true;
                worksheet.Cell(fila, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(fila, 4).Value = reporteGlobal.Sum(x => x.TotalSoles);
                worksheet.Cell(fila, 4).Style.Font.Bold = true;
                worksheet.Cell(fila, 4).Style.NumberFormat.Format = "#,##0.00";
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ReporteVentasTarjetas_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        private async Task<List<VentaTarjetaMensualDTO>> ObtenerDatosReporte(DateTime? desde, DateTime? hasta, string? nombreAlmacen)
        {
            var fechaInicio = (desde ?? DateTime.Today).Date;
            var fechaFin = (hasta ?? DateTime.Today).Date;
            if (fechaFin < fechaInicio) fechaFin = fechaInicio;
            var fechaFinExclusiva = fechaFin.AddDays(1);

            var fechaCorte = new DateTime(2025, 8, 10);
            var resultadosComb = new List<VentaTarjetaMensualDTO>();

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
                        cabecera.Fecha.Year,
                        cabecera.Fecha.Month,
                        AlmacenNombre = almacen.Nombre,
                        pago.Soles
                    }).ToListAsync();

                var groupedHist = queryHist
                    .GroupBy(x => new { x.Year, x.Month, x.AlmacenNombre })
                    .Select(g => new VentaTarjetaMensualDTO
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        AlmacenNombre = g.Key.AlmacenNombre,
                        TotalSoles = g.Sum(x => x.Soles)
                    });

                resultadosComb.AddRange(groupedHist);
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
                        cabecera.Fecha.Year,
                        cabecera.Fecha.Month,
                        AlmacenNombre = almacen.Nombre,
                        pago.Soles
                    }).ToListAsync();

                var groupedActual = queryActual
                    .GroupBy(x => new { x.Year, x.Month, x.AlmacenNombre })
                    .Select(g => new VentaTarjetaMensualDTO
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        AlmacenNombre = g.Key.AlmacenNombre,
                        TotalSoles = g.Sum(x => x.Soles)
                    });

                resultadosComb.AddRange(groupedActual);
            }

            return resultadosComb
                .GroupBy(x => new { x.Year, x.Month, x.AlmacenNombre })
                .Select(g => new VentaTarjetaMensualDTO
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    AlmacenNombre = g.Key.AlmacenNombre,
                    TotalSoles = g.Sum(x => x.TotalSoles)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ThenBy(x => x.AlmacenNombre)
                .ToList();
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