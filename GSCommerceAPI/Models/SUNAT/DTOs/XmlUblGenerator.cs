using GSCommerceAPI.Models.SUNAT.DTOs;
using System.Xml;
using System.Xml.Serialization;
using System.Text;

public static class XmlUblGenerator
{
    public static string GenerarFacturaUBL(ComprobanteCabeceraDTO cabecera)
    {
        // Aquí iría la estructura XML UBL 2.1
        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 });

        writer.WriteStartDocument();
        writer.WriteStartElement("Invoice", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");
        writer.WriteAttributeString("xmlns", "cac", null, "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
        writer.WriteAttributeString("xmlns", "cbc", null, "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");

        writer.WriteElementString("cbc:ID", $"{cabecera.Serie}-{cabecera.Numero:D8}");
        writer.WriteElementString("cbc:IssueDate", cabecera.FechaEmision.ToString("yyyy-MM-dd"));
        writer.WriteElementString("cbc:IssueTime", cabecera.HoraEmision.ToString(@"hh\:mm\:ss"));
        writer.WriteElementString("cbc:InvoiceTypeCode", cabecera.TipoDocumento == "BOLETA" ? "03" : "01");
        writer.WriteElementString("cbc:DocumentCurrencyCode", cabecera.Moneda);

        // Emisor
        writer.WriteStartElement("cac:AccountingSupplierParty");
        writer.WriteElementString("cbc:CustomerAssignedAccountID", cabecera.RucEmisor);
        writer.WriteElementString("cbc:AdditionalAccountID", "6"); // RUC
        writer.WriteStartElement("cac:Party");
        writer.WriteElementString("cbc:PartyName", cabecera.RazonSocialEmisor);
        writer.WriteElementString("cbc:StreetName", cabecera.DireccionEmisor);
        writer.WriteEndElement(); // Party
        writer.WriteEndElement(); // AccountingSupplierParty

        // Cliente
        writer.WriteStartElement("cac:AccountingCustomerParty");
        writer.WriteElementString("cbc:CustomerAssignedAccountID", cabecera.DocumentoCliente);
        writer.WriteElementString("cbc:AdditionalAccountID", cabecera.TipoDocumentoCliente); // 1 = DNI, 6 = RUC
        writer.WriteStartElement("cac:Party");
        writer.WriteElementString("cbc:PartyName", cabecera.NombreCliente);
        writer.WriteEndElement(); // Party
        writer.WriteEndElement(); // AccountingCustomerParty

        // Totales
        writer.WriteElementString("cbc:LegalMonetaryTotal", "");
        writer.WriteStartElement("cac:LegalMonetaryTotal");
        writer.WriteElementString("cbc:PayableAmount", cabecera.Total.ToString("0.00"));
        writer.WriteEndElement(); // LegalMonetaryTotal

        // Detalles
        foreach (var item in cabecera.Detalles)
        {
            writer.WriteStartElement("cac:InvoiceLine");
            writer.WriteElementString("cbc:ID", item.Item.ToString());
            writer.WriteElementString("cbc:InvoicedQuantity", item.Cantidad.ToString("0.00"));
            writer.WriteElementString("cbc:LineExtensionAmount", item.TotalSinIGV.ToString("0.00"));

            writer.WriteStartElement("cac:PricingReference");
            writer.WriteStartElement("cac:AlternativeConditionPrice");
            writer.WriteElementString("cbc:PriceAmount", item.PrecioUnitarioConIGV.ToString("0.00"));
            writer.WriteElementString("cbc:PriceTypeCode", "01"); // Precio con IGV
            writer.WriteEndElement(); // AlternativeConditionPrice
            writer.WriteEndElement(); // PricingReference

            writer.WriteStartElement("cac:Item");
            writer.WriteElementString("cbc:Description", item.DescripcionItem);
            writer.WriteEndElement(); // Item

            writer.WriteStartElement("cac:Price");
            writer.WriteElementString("cbc:PriceAmount", item.PrecioUnitarioSinIGV.ToString("0.00"));
            writer.WriteEndElement(); // Price

            writer.WriteEndElement(); // InvoiceLine
        }

        writer.WriteEndElement(); // Invoice
        writer.WriteEndDocument();

        return sb.ToString();
    }
}
