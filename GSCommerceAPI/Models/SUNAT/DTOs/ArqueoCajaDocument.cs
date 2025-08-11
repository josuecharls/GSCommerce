using GSCommerceAPI.Models.DTOs;
using GSCommerceAPI.Models.SUNAT.DTOs; // Ajusta si está en otro namespace
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;

public class ArqueoCajaDocument : IDocument
{
    private readonly ArqueoCajaDTO _dto;

    public ArqueoCajaDocument(ArqueoCajaDTO dto)
    {
        _dto = dto;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(11));

            page.Header().Column(header =>
            {
                header.Item().Text("ARQUEO DIARIO DE CAJA").Bold().FontSize(14).AlignCenter();
                header.Item().Text(_dto.Fecha.ToString("dd/MM/yyyy")).AlignCenter();
                header.Item().PaddingTop(5);

                header.Item().Row(row =>
                {
                    row.RelativeItem().Text(_dto.Empresa).Bold();
                    row.ConstantItem(150).Text(_dto.Cajero).AlignRight();
                });

                header.Item().Text(_dto.Sucursal);
                header.Item().PaddingVertical(5).LineHorizontal(1);
            });

            page.Content().Column(column =>
            {
                column.Spacing(15);

                column.Item().Text("Detalle").Bold().FontSize(12);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(3);
                        cols.ConstantColumn(100);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Grupo").Bold();
                        header.Cell().Text("Detalle").Bold();
                        header.Cell().AlignRight().Text("Total").Bold();
                    });

                    foreach (var item in _dto.Resumen)
                    {
                        table.Cell().Text(item.Grupo ?? string.Empty);
                        table.Cell().Text(item.Detalle ?? string.Empty);
                        table.Cell().AlignRight().Text(
                            item.Monto < 0
                                ? $"({Math.Abs(item.Monto):N2})"
                                : $"{item.Monto:N2}");
                    }
                });
                column.Item().PaddingVertical(5).LineHorizontal(1);
                column.Item().Text("Resumen").Bold().FontSize(12);

                column.Item().Table(table =>
                {
                    const float sep = 16f;               // espacio a cada lado de la línea
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();              // Ingresos
                        c.ConstantColumn(sep * 2 + 1);   // Separador: izq + línea + der
                        c.RelativeColumn();              // Egresos
                    });

                    // Ingresos (con respiración a la derecha)
                    table.Cell().PaddingRight(sep).Column(col =>
                    {
                        col.Item().Text("Ingresos").Bold();
                        var ventaEfectivo = _dto.VentasDelDia;
                        var ventaTotal = ventaEfectivo + _dto.VentaTarjeta + _dto.VentaNC;
                        col.Item().Row(r => { r.RelativeItem().Text("Saldo del día anterior (S/)"); r.ConstantItem(80).AlignRight().Text(_dto.SaldoInicial.ToString("N2")); });
                        col.Item().Row(r => { r.RelativeItem().Text("Venta en efectivo (S/)"); r.ConstantItem(80).AlignRight().Text(ventaEfectivo.ToString("N2")); });
                        col.Item().Row(r => { r.RelativeItem().Text("Venta con tarjeta (S/)"); r.ConstantItem(80).AlignRight().Text(_dto.VentaTarjeta.ToString("N2")); });
                        col.Item().Row(r => { r.RelativeItem().Text("Venta con N.C. (S/)"); r.ConstantItem(80).AlignRight().Text(_dto.VentaNC.ToString("N2")); });
                        col.Item().Row(r => { r.RelativeItem().Text("Otros ingresos (S/)"); r.ConstantItem(80).AlignRight().Text(_dto.Ingresos.ToString("N2")); });
                        col.Item().Row(r => { r.RelativeItem().Text("Total Ventas Dia (S/)").Bold(); r.ConstantItem(80).AlignRight().Text(ventaTotal.ToString("N2")).Bold(); });
                    });

                    // Separador: línea centrada dentro de la columna ancha
                    table.Cell().Element(e => e.ExtendVertical().AlignCenter().Width(1).LineVertical(1));

                    // Egresos (con respiración a la izquierda)
                    table.Cell().PaddingLeft(sep).Column(col =>
                    {
                        col.Item().Text("Egresos").Bold();
                        var totalEgresos = _dto.GastosDia + _dto.TransferenciasDia + _dto.PagosProveedores;

                        col.Item().Row(r => { r.RelativeItem().Text("Gastos del día (S/)"); r.ConstantItem(80).AlignRight().Text(_dto.GastosDia.ToString("N2")); });
                        col.Item().Row(r => { r.RelativeItem().Text("Transferencias del día (S/)"); r.ConstantItem(80).AlignRight().Text(_dto.TransferenciasDia.ToString("N2")); });
                        col.Item().Row(r => { r.RelativeItem().Text("Pagos a proveedores (S/)"); r.ConstantItem(80).AlignRight().Text(_dto.PagosProveedores.ToString("N2")); });
                        col.Item().Row(r => { r.RelativeItem().Text("Total egresos (S/)").Bold(); r.ConstantItem(80).AlignRight().Text(totalEgresos.ToString("N2")).Bold(); });
                        
                        column.Item().PaddingVertical(5).LineHorizontal(1);
                        column.Item().Border(1).Padding(8).Table(table =>
                        {
                            var saldoEnCaja = _dto.SaldoInicial + _dto.VentasDelDia + _dto.Ingresos - _dto.Egresos;
                            table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(100); });
                            table.Cell().Text("Saldo de caja (S/)").Bold();
                            table.Cell().AlignRight().Text(saldoEnCaja.ToString("N2"));
                            table.Cell().Text("Fondo fijo (S/)").Bold();
                            table.Cell().AlignRight().Text(_dto.FondoFijo.ToString("N2"));
                        });
                    });
                });
            });
        });
    }
}
