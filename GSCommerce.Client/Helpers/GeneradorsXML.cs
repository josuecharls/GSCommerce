using GSCommerce.Client.Models.SUNAT;
using GSCommerce.Client.Services;
using System.Text;

namespace GSCommerce.Client.Helpers { 

        public class GeneradorsXML
        {
            public string GenerarXMLDesdeDTO(ComprobanteCabeceraDTO comprobante)
            {
                var doc = new StringBuilder();
                string c = "\"";

                doc.AppendLine(@"<?xml version=" + c + "1.0" + c + " encoding=" + c + "UTF-8" + c + "?>");
                doc.AppendLine(@"<Invoice xmlns=" + c + "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2" + c + ">");
                doc.AppendLine($"<cbc:UBLVersionID>2.1</cbc:UBLVersionID>");
                doc.AppendLine($"<cbc:CustomizationID>2.0</cbc:CustomizationID>");
                doc.AppendLine($"<cbc:ID>{comprobante.Serie}-{comprobante.Numero.ToString("D8")}</cbc:ID>");
                doc.AppendLine($"<cbc:IssueDate>{comprobante.FechaEmision:yyyy-MM-dd}</cbc:IssueDate>");
                doc.AppendLine($"<cbc:IssueTime>{comprobante.HoraEmision}</cbc:IssueTime>");
                doc.AppendLine($"<cbc:InvoiceTypeCode>{comprobante.TipoDocumento}</cbc:InvoiceTypeCode>");
                doc.AppendLine($"<cbc:Note languageLocaleID=\"1000\"><![CDATA[{comprobante.MontoLetras}]]></cbc:Note>");
                doc.AppendLine($"<cbc:DocumentCurrencyCode>{comprobante.Moneda}</cbc:DocumentCurrencyCode>");
                doc.AppendLine($"<cbc:LineCountNumeric>{comprobante.Detalles.Count}</cbc:LineCountNumeric>");

            // Firma Digital
            doc.AppendLine("<cac:Signature>");
            doc.AppendLine($"<cbc:ID>SF{comprobante.Serie}-{comprobante.Numero.ToString("D8")}</cbc:ID>");
            doc.AppendLine("<cac:SignatoryParty>");
            doc.AppendLine("<cac:PartyIdentification>");
            doc.AppendLine($"<cbc:ID>{comprobante.RucEmisor}</cbc:ID>");
            doc.AppendLine("</cac:PartyIdentification>");
            doc.AppendLine("<cac:PartyName>");
            doc.AppendLine($"<cbc:Name>{comprobante.RazonSocialEmisor}</cbc:Name>");
            doc.AppendLine("</cac:PartyName>");
            doc.AppendLine("</cac:SignatoryParty>");
            doc.AppendLine("<cac:DigitalSignatureAttachment>");
            doc.AppendLine("<cac:ExternalReference>");
            doc.AppendLine($"<cbc:URI>#SF{comprobante.Serie}-{comprobante.Numero.ToString("D8")}</cbc:URI>");
            doc.AppendLine("</cac:ExternalReference>");
            doc.AppendLine("</cac:DigitalSignatureAttachment>");
            doc.AppendLine("</cac:Signature>");
            //emisor
            doc.AppendLine("<cac:AccountingSupplierParty>");
            doc.AppendLine($"<cbc:CustomerAssignedAccountID>{comprobante.RucEmisor}</cbc:CustomerAssignedAccountID>");
            doc.AppendLine("<cbc:AdditionalAccountID>6</cbc:AdditionalAccountID>"); // 6 = RUC

            doc.AppendLine("<cac:Party>");
            doc.AppendLine("<cac:PartyLegalEntity>");
            doc.AppendLine($"<cbc:RegistrationName>{comprobante.RazonSocialEmisor}</cbc:RegistrationName>");
            doc.AppendLine("<cac:RegistrationAddress>");
            doc.AppendLine($"<cbc:ID>{comprobante.UbigeoEmisor}</cbc:ID>");
            doc.AppendLine("<cbc:AddressTypeCode>0000</cbc:AddressTypeCode>");
            doc.AppendLine($"<cbc:StreetName>{comprobante.DireccionEmisor}</cbc:StreetName>");
            doc.AppendLine("<cac:Country><cbc:IdentificationCode>PE</cbc:IdentificationCode></cac:Country>");
            doc.AppendLine("</cac:RegistrationAddress>");
            doc.AppendLine("</cac:PartyLegalEntity>");
            doc.AppendLine("</cac:Party>");
            doc.AppendLine("</cac:AccountingSupplierParty>");

            //Cliente
            doc.AppendLine("<cac:AccountingCustomerParty>");
            doc.AppendLine($"<cbc:CustomerAssignedAccountID>{comprobante.DocumentoCliente}</cbc:CustomerAssignedAccountID>");
            doc.AppendLine($"<cbc:AdditionalAccountID>{comprobante.TipoDocumentoCliente}</cbc:AdditionalAccountID>"); // 6 = RUC, 1 = DNI

            doc.AppendLine("<cac:Party>");
            doc.AppendLine("<cac:PartyLegalEntity>");
            doc.AppendLine($"<cbc:RegistrationName>{comprobante.NombreCliente}</cbc:RegistrationName>");
            doc.AppendLine("<cac:RegistrationAddress>");
            doc.AppendLine($"<cbc:StreetName>{comprobante.DireccionCliente}</cbc:StreetName>");
            doc.AppendLine("<cac:Country><cbc:IdentificationCode>PE</cbc:IdentificationCode></cac:Country>");
            doc.AppendLine("</cac:RegistrationAddress>");
            doc.AppendLine("</cac:PartyLegalEntity>");
            doc.AppendLine("</cac:Party>");
            doc.AppendLine("</cac:AccountingCustomerParty>");

            int contadorItem = 1;

            foreach (var detalle in comprobante.Detalles)
            {
                doc.AppendLine("<cac:InvoiceLine>");
                doc.AppendLine($"<cbc:ID>{contadorItem}</cbc:ID>");
                doc.AppendLine($"<cbc:InvoicedQuantity unitCode=\"{detalle.UnidadMedida}\">{detalle.Cantidad}</cbc:InvoicedQuantity>");
                doc.AppendLine($"<cbc:LineExtensionAmount currencyID=\"PEN\">{detalle.TotalSinIGV:F2}</cbc:LineExtensionAmount>");
                // Referencia de precios
                doc.AppendLine("<cac:PricingReference>");
                doc.AppendLine("<cac:AlternativeConditionPrice>");
                doc.AppendLine($"<cbc:PriceAmount currencyID=\"PEN\">{detalle.PrecioUnitarioConIGV:F2}</cbc:PriceAmount>");
                doc.AppendLine("<cbc:PriceTypeCode>01</cbc:PriceTypeCode>"); // Precio unitario con IGV
                doc.AppendLine("</cac:AlternativeConditionPrice>");
                doc.AppendLine("</cac:PricingReference>");

                // Total de impuestos por ítem
                doc.AppendLine("<cac:TaxTotal>");
                doc.AppendLine($"<cbc:TaxAmount currencyID=\"PEN\">{detalle.IGV:F2}</cbc:TaxAmount>");
                doc.AppendLine("<cac:TaxSubtotal>");
                doc.AppendLine($"<cbc:TaxableAmount currencyID=\"PEN\">{detalle.TotalSinIGV:F2}</cbc:TaxableAmount>");
                doc.AppendLine($"<cbc:TaxAmount currencyID=\"PEN\">{detalle.IGV:F2}</cbc:TaxAmount>");
                doc.AppendLine("<cac:TaxCategory>");    
                doc.AppendLine("<cbc:Percent>18.00</cbc:Percent>");
                doc.AppendLine("<cbc:TaxExemptionReasonCode>10</cbc:TaxExemptionReasonCode>"); // Gravado - IGV
                doc.AppendLine("<cac:TaxScheme>");
                doc.AppendLine("<cbc:ID>1000</cbc:ID>");
                doc.AppendLine("<cbc:Name>IGV</cbc:Name>");
                doc.AppendLine("<cbc:TaxTypeCode>VAT</cbc:TaxTypeCode>");
                doc.AppendLine("</cac:TaxScheme>");
                doc.AppendLine("</cac:TaxCategory>");
                doc.AppendLine("</cac:TaxSubtotal>");
                doc.AppendLine("</cac:TaxTotal>");

                // Información del producto
                doc.AppendLine("<cac:Item>");
                doc.AppendLine($"<cbc:Description>{detalle.DescripcionItem}</cbc:Description>");
                doc.AppendLine("<cac:SellersItemIdentification>");
                doc.AppendLine($"<cbc:ID>{detalle.CodigoItem}</cbc:ID>");
                doc.AppendLine("</cac:SellersItemIdentification>");
                doc.AppendLine("</cac:Item>");

                // Precio sin IGV
                doc.AppendLine("<cac:Price>");
                doc.AppendLine($"<cbc:PriceAmount currencyID=\"PEN\">{detalle.PrecioUnitarioSinIGV:F2}</cbc:PriceAmount>");
                doc.AppendLine("</cac:Price>");

                doc.AppendLine("</cac:InvoiceLine>");
            }

            doc.AppendLine("<cac:LegalMonetaryTotal>");
            doc.AppendLine($"<cbc:LineExtensionAmount currencyID=\"PEN\">{comprobante.SubTotal:F2}</cbc:LineExtensionAmount>");
            doc.AppendLine($"<cbc:TaxInclusiveAmount currencyID=\"PEN\">{comprobante.Total:F2}</cbc:TaxInclusiveAmount>");
            doc.AppendLine($"<cbc:PayableAmount currencyID=\"PEN\">{comprobante.Total:F2}</cbc:PayableAmount>");
            doc.AppendLine("</cac:LegalMonetaryTotal>");


            doc.AppendLine("</Invoice>");
                return doc.ToString();
            }
        }
}
