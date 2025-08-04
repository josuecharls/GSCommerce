using GSCommerceAPI.Models.DTOs;
using GSCommerceAPI.Models.SUNAT.DTOs; // Ajusta si está en otro namespace
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
        container
            .Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Column(header =>
                {
                    header.Item().Text(_dto.Empresa).FontSize(16).Bold().AlignCenter();
                    header.Item().Text($"Sucursal: {_dto.Sucursal}").AlignCenter();
                    header.Item().Text($"Fecha: {_dto.Fecha:dd/MM/yyyy}").AlignCenter();
                    header.Item().Text($"Cajero: {_dto.Cajero}").AlignCenter();
                    header.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(15);

                    column.Item().Text("Detalle de Movimientos").Bold().FontSize(14);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Grupo").Bold();
                            header.Cell().Text("Detalle").Bold();
                            header.Cell().AlignRight().Text("Total").Bold();
                        });

                        foreach (var item in _dto.Resumen)
                        {
                            table.Cell().Text(item.Grupo);
                            table.Cell().Text(item.Detalle);
                            table.Cell().AlignRight().Text($"S/. {item.Monto:N2}");
                        }
                    });

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken2);

                    column.Item().Text("Resumen").Bold().FontSize(14);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        table.Cell().Column(col =>
                        {
                            col.Item().Text("Ingresos").Bold();
                            col.Item().Row(row =>
                            {
                                row.RelativeColumn().Text("Saldo del día anterior");
                                row.ConstantColumn(100).AlignRight().Text($"S/. {_dto.SaldoDiaAnterior:N2}");
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeColumn().Text("Ventas del día");
                                row.ConstantColumn(100).AlignRight().Text($"S/. {_dto.VentasDelDia:N2}");
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeColumn().Text("Otros ingresos");
                                row.ConstantColumn(100).AlignRight().Text($"S/. {_dto.OtrosIngresos:N2}");
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeColumn().Text("Venta con tarjeta");
                                row.ConstantColumn(100).AlignRight().Text($"S/. {_dto.VentaTarjeta:N2}");
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeColumn().Text("Venta con N.C.");
                                row.ConstantColumn(100).AlignRight().Text($"S/. {_dto.VentaNC:N2}");
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeColumn().Text("Total ingresos").Bold();
                                row.ConstantColumn(100).AlignRight().Text($"S/. {_dto.Ingresos:N2}").Bold();
                            });
                        });

                        table.Cell().Column(col =>
                        {
                            col.Item().Text("Egresos").Bold();
                            col.Item().Row(row =>
                            {
                                row.RelativeColumn().Text("Gastos del día");
                                row.ConstantColumn(100).AlignRight().Text($"S/. {_dto.GastosDia:N2}");
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeColumn().Text("Transferencias del día");
                                row.ConstantColumn(100).AlignRight().Text($"S/. {_dto.TransferenciasDia:N2}");
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeColumn().Text("Pagos a proveedores");
                                row.ConstantColumn(100).AlignRight().Text($"S/. {_dto.PagosProveedores:N2}");
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeColumn().Text("Total egresos").Bold();
                                row.ConstantColumn(100).AlignRight().Text($"S/. {_dto.Egresos:N2}").Bold();
                            });
                        });
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(100);
                        });

                        table.Cell().Text("Saldo de caja").Bold();
                        table.Cell().AlignRight().Text($"S/. {_dto.SaldoFinal:N2}");
                        table.Cell().Text("Fondo fijo").Bold();
                        table.Cell().AlignRight().Text($"S/. {_dto.FondoFijo:N2}");
                    });

                    column.Item().PaddingTop(40).LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                    column.Item().Text("Firma del Cajero").AlignCenter().Italic();
                });
            });
    }
}