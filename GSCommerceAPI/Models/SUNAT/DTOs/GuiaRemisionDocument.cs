using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using GSCommerceAPI.Models.SUNAT.DTOs;

namespace GSCommerceAPI.Models.SUNAT.DTOs;

/// <summary>
/// Documento PDF para Guía de Remisión Electrónica
/// </summary>
public class GuiaRemisionDocument : IDocument
{
    private readonly GuiaRemisionCabeceraDTO _guia;
    private readonly string? _qrData;

    public GuiaRemisionDocument(GuiaRemisionCabeceraDTO guia, string? qrData = null)
    {
        _guia = guia;
        _qrData = qrData;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(25);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            // Fila principal con logo/razón social y datos del documento
            column.Item().Row(row =>
            {
                // Columna izquierda: Razón social y datos del remitente
                row.RelativeItem(3).Column(col =>
                {
                    col.Item().Text(_guia.RazonSocialRemitente).Bold().FontSize(12);
                    col.Item().PaddingTop(5).Text($"Fecha y hora de emisión: {_guia.FechaEmision:dd/MM/yyyy HH:mm} PM").FontSize(8);
                });

                // Columna derecha: Cuadro con datos del documento
                row.ConstantItem(180).Border(1).BorderColor(Colors.Blue.Darken2).Padding(8).Column(col =>
                {
                    col.Item().Text($"RUC N° {_guia.RucRemitente}").Bold().FontSize(10).AlignCenter();
                    col.Item().PaddingTop(3).Text("GUÍA DE REMISIÓN ELECTRÓNICA").Bold().FontSize(9).FontColor(Colors.Blue.Darken2).AlignCenter();
                    col.Item().Text("REMITENTE").Bold().FontSize(9).FontColor(Colors.Blue.Darken2).AlignCenter();
                    col.Item().PaddingTop(3).Text($"N° {_guia.Serie} - {_guia.Numero:D8}").Bold().FontSize(10).AlignCenter();
                });
            });

            column.Item().PaddingTop(10);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(5);

            // Obtener descripción del motivo de traslado
            var motivoDesc = CatalogoMotivoTraslado.Motivos.TryGetValue(_guia.MotivoTraslado, out var desc) ? desc : _guia.MotivoTraslado;

            // Motivo de traslado
            column.Item().Row(row =>
            {
                row.ConstantItem(130).Text("Motivo de traslado:").Bold();
                row.RelativeItem().Text(motivoDesc);
            });

            // Puntos de partida y llegada
            column.Item().PaddingTop(5).Row(row =>
            {
                row.ConstantItem(130).Text("Punto de partida:").Bold();
                row.RelativeItem().Text($"{_guia.DireccionPartida}");
            });

            column.Item().Row(row =>
            {
                row.ConstantItem(130).Text("Punto de llegada:").Bold();
                row.RelativeItem().Text($"{_guia.DireccionLlegada}");
            });

            // Datos del destinatario
            column.Item().PaddingTop(8).Row(row =>
            {
                row.ConstantItem(130).Text("Datos del destinatario:").Bold();
                var tipoDoc = CatalogoTipoDocumentoIdentidad.Tipos.TryGetValue(_guia.TipoDocumentoDestinatario, out var tipoDocDesc) ? tipoDocDesc : "RUC";
                row.RelativeItem().Text($"{_guia.RazonSocialDestinatario} - {tipoDoc} N° {_guia.NumeroDocumentoDestinatario}");
            });

            // Documentos relacionados (si hay factura o similar)
            column.Item().PaddingTop(5).Text("Documentos relacionados:").Bold();

            // Bienes por transportar
            column.Item().PaddingTop(8).Text("Bienes por transportar").Bold().FontSize(10).FontColor(Colors.Blue.Darken2);

            // Tabla de bienes
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(25);  // N°
                    cols.ConstantColumn(50);  // Bien normalizado
                    cols.ConstantColumn(60);  // Código bien
                    cols.ConstantColumn(60);  // Código producto SUNAT
                    cols.ConstantColumn(70);  // Código subpartida nacional
                    cols.ConstantColumn(50);  // Código GTIN
                    cols.RelativeColumn();    // Descripción detallada
                    cols.ConstantColumn(55);  // Unidad medida
                    cols.ConstantColumn(55);  // Cantidad
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Border(0.5f).Padding(3).Text("N°").Bold().FontSize(7).AlignCenter();
                    header.Cell().Background(Colors.Grey.Lighten3).Border(0.5f).Padding(3).Text("Bien\nnormalizado").Bold().FontSize(7).AlignCenter();
                    header.Cell().Background(Colors.Grey.Lighten3).Border(0.5f).Padding(3).Text("Código\nbien").Bold().FontSize(7).AlignCenter();
                    header.Cell().Background(Colors.Grey.Lighten3).Border(0.5f).Padding(3).Text("Código\nproducto\nSUNAT").Bold().FontSize(7).AlignCenter();
                    header.Cell().Background(Colors.Grey.Lighten3).Border(0.5f).Padding(3).Text("Código\nsubpartida\nnacional").Bold().FontSize(7).AlignCenter();
                    header.Cell().Background(Colors.Grey.Lighten3).Border(0.5f).Padding(3).Text("Código\nGTIN").Bold().FontSize(7).AlignCenter();
                    header.Cell().Background(Colors.Grey.Lighten3).Border(0.5f).Padding(3).Text("Descripción\ndetallada").Bold().FontSize(7).AlignCenter();
                    header.Cell().Background(Colors.Grey.Lighten3).Border(0.5f).Padding(3).Text("Unidad de\nmedida").Bold().FontSize(7).AlignCenter();
                    header.Cell().Background(Colors.Grey.Lighten3).Border(0.5f).Padding(3).Text("Cantidad").Bold().FontSize(7).AlignCenter();
                });

                // Rows
                var itemNum = 1;
                foreach (var det in _guia.Detalles)
                {
                    table.Cell().Border(0.5f).Padding(3).Text(itemNum.ToString()).FontSize(8).AlignCenter();
                    table.Cell().Border(0.5f).Padding(3).Text("NO").FontSize(8).AlignCenter();
                    table.Cell().Border(0.5f).Padding(3).Text(det.IdArticulo ?? "").FontSize(8).AlignCenter();
                    table.Cell().Border(0.5f).Padding(3).Text("").FontSize(8).AlignCenter();
                    table.Cell().Border(0.5f).Padding(3).Text("").FontSize(8).AlignCenter();
                    table.Cell().Border(0.5f).Padding(3).Text("").FontSize(8).AlignCenter();
                    table.Cell().Border(0.5f).Padding(3).Text(det.DescripcionArticulo ?? "").FontSize(8);
                    table.Cell().Border(0.5f).Padding(3).Text(ObtenerUnidadMedida(det.UnidadMedida)).FontSize(8).AlignCenter();
                    table.Cell().Border(0.5f).Padding(3).Text(det.Cantidad.ToString("N0")).FontSize(8).AlignCenter();
                    itemNum++;
                }
            });

            // Datos del traslado
            column.Item().PaddingTop(10).Row(row =>
            {
                row.ConstantItem(180).Text("Unidad de medida del peso bruto:").Bold();
                row.RelativeItem().Text("KGM");
            });

            column.Item().Row(row =>
            {
                row.ConstantItem(180).Text("Peso bruto total de la carga:").Bold();
                row.RelativeItem().Text($"{_guia.PesoBrutoTotal:N3}");
                row.ConstantItem(250).Text("Sustento de la diferencia del peso bruto total respecto a lo seleccionado:");
            });

            column.Item().PaddingTop(8).Text("Datos del traslado").Bold().FontSize(10).FontColor(Colors.Blue.Darken2);

            // Modalidad
            var modalidadDesc = CatalogoModalidadTransporte.Modalidades.TryGetValue(_guia.ModalidadTraslado, out var modDesc) ? modDesc : _guia.ModalidadTraslado;
            column.Item().Row(row =>
            {
                row.ConstantItem(180).Text("Modalidad de traslado:").Bold();
                row.RelativeItem().Text(modalidadDesc);
            });

            column.Item().Row(row =>
            {
                row.ConstantItem(250).Text("Indicador de transbordo programado:").Bold();
                row.RelativeItem().Text("No");
            });

            column.Item().Row(row =>
            {
                row.ConstantItem(350).Text("Indicador traslado en vehículos de categoría M1 o L:").Bold();
                row.RelativeItem().Text(_guia.EsVehiculoM1L ? "Sí" : "No");
            });

            // Vehículo y conductor (si aplica)
            if (_guia.ModalidadTraslado == "02" && !string.IsNullOrEmpty(_guia.PlacaVehiculo))
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(180).Text("Placa del vehículo:").Bold();
                    row.RelativeItem().Text(_guia.PlacaVehiculo);
                });
            }

            // Observaciones
            if (!string.IsNullOrEmpty(_guia.Observaciones))
            {
                column.Item().PaddingTop(8).Text("Observaciones").Bold().FontSize(10).FontColor(Colors.Blue.Darken2);
                column.Item().Text(_guia.Observaciones);
            }
            else
            {
                column.Item().PaddingTop(8).Text("Observaciones").Bold().FontSize(10).FontColor(Colors.Blue.Darken2);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(8).Text("Esta es una representación impresa sin valor tributario de la Guía de Remisión Electrónica, generada en el sistema de la SUNAT. Puede verificarla utilizando su clave SOL")
                .FontSize(8).FontColor(Colors.Grey.Darken2).AlignCenter();
        });
    }

    private static string ObtenerUnidadMedida(string? codigo)
    {
        return codigo switch
        {
            "NIU" => "NIU",
            "KGM" => "KGM",
            "MTR" => "MTR",
            "LTR" => "LTR",
            _ => codigo ?? "NIU"
        };
    }
}
