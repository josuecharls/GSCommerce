using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using GSCommerceAPI.Models.DTOs;
using GSCommerceAPI.Models.SUNAT.DTOs; // Ajusta si está en otro namespace

public class ArqueoCajaDocument : IDocument
{
    private readonly ArqueoCajaDTO _dto;

    public  ArqueoCajaDocument(ArqueoCajaDTO dto)
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
                page.Content().Column(column =>
                {
                    column.Spacing(15);

                    // Título principal
                    column.Item().Text($"ARQUEO DE CAJA - {_dto.Fecha:dd/MM/yyyy}")
                        .FontSize(20).Bold().AlignCenter();

                    // Datos generales
                    column.Item().Text($"📅 Fecha: {_dto.Fecha:dd/MM/yyyy}");
                    column.Item().Text($"👤 Usuario: {_dto.Usuario}");
                    column.Item().Text($"🧾 Cajero: {_dto.Cajero}");
                    column.Item().Text($"📝 Observación: {_dto.ObservacionCierre ?? "-"}");

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken2);

                    // Título del resumen
                    column.Item().Text("Resumen de Movimientos").Bold().FontSize(14);

                    // Tabla del resumen
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
                            header.Cell().Text("Monto").Bold().AlignRight();
                        });

                        foreach (var item in _dto.Resumen)
                        {
                            table.Cell().Text(item.Grupo);
                            table.Cell().Text(item.Detalle);
                            table.Cell().Text($"S/. {item.Monto:N2}").AlignRight();
                        }
                    });

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken2);

                    // Totales
                    column.Item().PaddingTop(10).Column(sub =>
                    {
                        sub.Spacing(3);

                        sub.Item().Text($"💵 Saldo Inicial: S/. {_dto.SaldoInicial:N2}");
                        sub.Item().Text($"➕ Ingresos: S/. {_dto.Ingresos:N2}");
                        sub.Item().Text($"➖ Egresos: S/. {_dto.Egresos:N2}");
                        sub.Item().Text($"🛒 Venta del Día: S/. {_dto.VentaDia:N2}");
                        sub.Item().Text($"💰 Saldo Final: S/. {_dto.SaldoFinal:N2}").Bold();
                    });

                    column.Item().PaddingTop(40).LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                    column.Item().Text("Firma del Cajero").AlignCenter().Italic();
                });
            });
    }
}
