using System.Collections.Generic;

namespace GSCommerceAPI.Models.DTOs
{
    public class NotaCreditoPDFDTO
    {
        public string Serie { get; set; } = string.Empty;
        public int Numero { get; set; }
        public DateOnly Fecha { get; set; }
        public string Referencia { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string Dniruc { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public List<NotaCreditoPDFDetalleDTO> Detalles { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
    }
}