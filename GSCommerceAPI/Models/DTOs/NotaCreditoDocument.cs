using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GSCommerceAPI.Models.DTOs
{
    public class NotaCreditoDocument : IDocument
    {
        private readonly NotaCreditoPDFDTO _dto;

        public NotaCreditoDocument(NotaCreditoPDFDTO dto)
        {
            _dto = dto;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text("NOTA DE CRÉDITO").FontSize(20).Bold().AlignCenter();
                    column.Item().Text($"Serie: {_dto.Serie}-{_dto.Numero:D8}");
                    column.Item().Text($"Fecha: {_dto.Fecha:dd/MM/yyyy}");
                    column.Item().Text($"Cliente: {_dto.Cliente}");
                    column.Item().Text($"Documento: {_dto.Dniruc}");
                    column.Item().Text($"Dirección: {_dto.Direccion}");
                    column.Item().Text($"Referencia: {_dto.Referencia}");

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken2);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(40);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(60);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Descripción").Bold();
                            header.Cell().Text("Cant").Bold();
                            header.Cell().Text("Precio").Bold().AlignRight();
                            header.Cell().Text("Total").Bold().AlignRight();
                        });

                        foreach (var d in _dto.Detalles)
                        {
                            table.Cell().Text(d.Descripcion);
                            table.Cell().Text(d.Cantidad.ToString());
                            table.Cell().Text($"{d.Precio:N2}").AlignRight();
                            table.Cell().Text($"{d.Total:N2}").AlignRight();
                        }
                    });

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken2);

                    column.Item().AlignRight().Column(sub =>
                    {
                        sub.Item().Text($"Subtotal: S/. {_dto.SubTotal:N2}");
                        sub.Item().Text($"IGV: S/. {_dto.Igv:N2}");
                        sub.Item().Text($"Total: S/. {_dto.Total:N2}").Bold();
                    });
                });
            });
        }
    }
}