using GSCommerce.Client.Models.SUNAT;
using System.Text;

namespace GSCommerce.Client.Services
{
    public class SunatXmlService
    {
        public string GenerarXmlFactura(ComprobanteCabeceraDTO comprobante)
        {
            var c = "\""; // comillas para atributos XML
            var sb = new StringBuilder();

            string serieNumero = comprobante.Serie + "-" + comprobante.Numero.ToString("00000000");

            sb.AppendLine($"<?xml version={c}1.0{c} encoding={c}ISO-8859-1{c} standalone={c}no{c}?>");
            sb.AppendLine($"<Invoice xmlns={c}urn:oasis:names:specification:ubl:schema:xsd:Invoice-2{c}");
            sb.AppendLine($"    xmlns:cbc={c}urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2{c}");
            sb.AppendLine($"    xmlns:cac={c}urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2{c}");
            sb.AppendLine($"    xmlns:ext={c}urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2{c}>");

            // UBL Extensions vacías para la firma
            sb.AppendLine("  <ext:UBLExtensions>");
            sb.AppendLine("    <ext:UBLExtension><ext:ExtensionContent/></ext:UBLExtension>");
            sb.AppendLine("  </ext:UBLExtensions>");

            // Cabecera básica
            sb.AppendLine($"  <cbc:UBLVersionID>2.1</cbc:UBLVersionID>");
            sb.AppendLine($"  <cbc:CustomizationID>2.0</cbc:CustomizationID>");
            sb.AppendLine($"  <cbc:ID>{serieNumero}</cbc:ID>");
            sb.AppendLine($"  <cbc:IssueDate>{comprobante.FechaEmision:yyyy-MM-dd}</cbc:IssueDate>");
            sb.AppendLine($"  <cbc:IssueTime>{comprobante.HoraEmision}</cbc:IssueTime>");
            sb.AppendLine($"  <cbc:InvoiceTypeCode>01</cbc:InvoiceTypeCode>"); // 01 = factura
            sb.AppendLine($"  <cbc:DocumentCurrencyCode>{comprobante.Moneda}</cbc:DocumentCurrencyCode>");

            // Firma electrónica
            sb.AppendLine("  <cac:Signature>");
            sb.AppendLine($"    <cbc:ID>SF{serieNumero}</cbc:ID>");
            sb.AppendLine("    <cac:SignatoryParty>");
            sb.AppendLine($"      <cac:PartyIdentification><cbc:ID>{comprobante.RucEmisor}</cbc:ID></cac:PartyIdentification>");
            sb.AppendLine($"      <cac:PartyName><cbc:Name>{comprobante.RazonSocialEmisor}</cbc:Name></cac:PartyName>");
            sb.AppendLine("    </cac:SignatoryParty>");
            sb.AppendLine("    <cac:DigitalSignatureAttachment>");
            sb.AppendLine($"      <cac:ExternalReference><cbc:URI>#SF{serieNumero}</cbc:URI></cac:ExternalReference>");
            sb.AppendLine("    </cac:DigitalSignatureAttachment>");
            sb.AppendLine("  </cac:Signature>");

            // Emisor
            sb.AppendLine("  <cac:AccountingSupplierParty>");
            sb.AppendLine($"    <cbc:CustomerAssignedAccountID>{comprobante.RucEmisor}</cbc:CustomerAssignedAccountID>");
            sb.AppendLine("    <cbc:AdditionalAccountID>6</cbc:AdditionalAccountID>");
            sb.AppendLine("    <cac:Party>");
            sb.AppendLine($"      <cac:PartyLegalEntity>");
            sb.AppendLine($"        <cbc:RegistrationName>{comprobante.RazonSocialEmisor}</cbc:RegistrationName>");
            sb.AppendLine($"      </cac:PartyLegalEntity>");
            sb.AppendLine("    </cac:Party>");
            sb.AppendLine("  </cac:AccountingSupplierParty>");

            // Cliente
            sb.AppendLine("  <cac:AccountingCustomerParty>");
            sb.AppendLine($"    <cbc:CustomerAssignedAccountID>{comprobante.DocumentoCliente}</cbc:CustomerAssignedAccountID>");
            sb.AppendLine($"    <cbc:AdditionalAccountID>{comprobante.TipoDocumentoCliente}</cbc:AdditionalAccountID>");
            sb.AppendLine("    <cac:Party>");
            sb.AppendLine($"      <cac:PartyLegalEntity><cbc:RegistrationName>{comprobante.NombreCliente}</cbc:RegistrationName></cac:PartyLegalEntity>");
            sb.AppendLine("    </cac:Party>");
            sb.AppendLine("  </cac:AccountingCustomerParty>");

            // Detalle (se completará en el siguiente paso)
            // Totales e IGV
            sb.AppendLine("  <cac:TaxTotal>");
            sb.AppendLine($"    <cbc:TaxAmount currencyID={c}{comprobante.Moneda}{c}>{comprobante.Igv.ToString("F2")}</cbc:TaxAmount>");
            sb.AppendLine("    <cac:TaxSubtotal>");
            sb.AppendLine($"      <cbc:TaxableAmount currencyID={c}{comprobante.Moneda}{c}>{comprobante.SubTotal.ToString("F2")}</cbc:TaxableAmount>");
            sb.AppendLine($"      <cbc:TaxAmount currencyID={c}{comprobante.Moneda}{c}>{comprobante.Igv.ToString("F2")}</cbc:TaxAmount>");
            sb.AppendLine("      <cac:TaxCategory>");
            sb.AppendLine("        <cac:TaxScheme>");
            sb.AppendLine("          <cbc:ID>1000</cbc:ID>");
            sb.AppendLine("          <cbc:Name>IGV</cbc:Name>");
            sb.AppendLine("          <cbc:TaxTypeCode>VAT</cbc:TaxTypeCode>");
            sb.AppendLine("        </cac:TaxScheme>");
            sb.AppendLine("      </cac:TaxCategory>");
            sb.AppendLine("    </cac:TaxSubtotal>");
            sb.AppendLine("  </cac:TaxTotal>");

            sb.AppendLine("  <cac:LegalMonetaryTotal>");
            sb.AppendLine($"    <cbc:LineExtensionAmount currencyID={c}{comprobante.Moneda}{c}>{comprobante.SubTotal.ToString("F2")}</cbc:LineExtensionAmount>");
            sb.AppendLine($"    <cbc:TaxInclusiveAmount currencyID={c}{comprobante.Moneda}{c}>{comprobante.Total.ToString("F2")}</cbc:TaxInclusiveAmount>");
            sb.AppendLine($"    <cbc:PayableAmount currencyID={c}{comprobante.Moneda}{c}>{comprobante.Total.ToString("F2")}</cbc:PayableAmount>");
            sb.AppendLine("  </cac:LegalMonetaryTotal>");

            foreach (var item in comprobante.Detalles)
            {
                sb.AppendLine("  <cac:InvoiceLine>");
                sb.AppendLine($"    <cbc:ID>{item.Item}</cbc:ID>");
                sb.AppendLine($"    <cbc:InvoicedQuantity unitCode={c}{item.UnidadMedida}{c}>{item.Cantidad.ToString("F2")}</cbc:InvoicedQuantity>");
                sb.AppendLine($"    <cbc:LineExtensionAmount currencyID={c}{comprobante.Moneda}{c}>{item.TotalSinIGV.ToString("F2")}</cbc:LineExtensionAmount>");

                sb.AppendLine("    <cac:PricingReference>");
                sb.AppendLine("      <cac:AlternativeConditionPrice>");
                sb.AppendLine($"        <cbc:PriceAmount currencyID={c}{comprobante.Moneda}{c}>{item.PrecioUnitarioConIGV.ToString("F2")}</cbc:PriceAmount>");
                sb.AppendLine("        <cbc:PriceTypeCode>01</cbc:PriceTypeCode>");
                sb.AppendLine("      </cac:AlternativeConditionPrice>");
                sb.AppendLine("    </cac:PricingReference>");

                sb.AppendLine("    <cac:TaxTotal>");
                sb.AppendLine($"      <cbc:TaxAmount currencyID={c}{comprobante.Moneda}{c}>{item.IGV.ToString("F2")}</cbc:TaxAmount>");
                sb.AppendLine("      <cac:TaxSubtotal>");
                sb.AppendLine($"        <cbc:TaxableAmount currencyID={c}{comprobante.Moneda}{c}>{item.TotalSinIGV.ToString("F2")}</cbc:TaxableAmount>");
                sb.AppendLine($"        <cbc:TaxAmount currencyID={c}{comprobante.Moneda}{c}>{item.IGV.ToString("F2")}</cbc:TaxAmount>");
                sb.AppendLine("        <cac:TaxCategory>");
                sb.AppendLine("          <cbc:Percent>18</cbc:Percent>");
                sb.AppendLine("          <cbc:TaxExemptionReasonCode>10</cbc:TaxExemptionReasonCode>");
                sb.AppendLine("          <cac:TaxScheme>");
                sb.AppendLine("            <cbc:ID>1000</cbc:ID>");
                sb.AppendLine("            <cbc:Name>IGV</cbc:Name>");
                sb.AppendLine("            <cbc:TaxTypeCode>VAT</cbc:TaxTypeCode>");
                sb.AppendLine("          </cac:TaxScheme>");
                sb.AppendLine("        </cac:TaxCategory>");
                sb.AppendLine("      </cac:TaxSubtotal>");
                sb.AppendLine("    </cac:TaxTotal>");

                sb.AppendLine("    <cac:Item>");
                sb.AppendLine($"      <cbc:Description>{item.DescripcionItem}</cbc:Description>");
                sb.AppendLine("      <cac:SellersItemIdentification>");
                sb.AppendLine($"        <cbc:ID>{item.CodigoItem}</cbc:ID>");
                sb.AppendLine("      </cac:SellersItemIdentification>");
                sb.AppendLine("    </cac:Item>");

                sb.AppendLine("    <cac:Price>");
                sb.AppendLine($"      <cbc:PriceAmount currencyID={c}{comprobante.Moneda}{c}>{item.PrecioUnitarioSinIGV.ToString("F2")}</cbc:PriceAmount>");
                sb.AppendLine("    </cac:Price>");

                sb.AppendLine("  </cac:InvoiceLine>");
            }

            sb.AppendLine("</Invoice>");

            return sb.ToString();
        }
    }
}
