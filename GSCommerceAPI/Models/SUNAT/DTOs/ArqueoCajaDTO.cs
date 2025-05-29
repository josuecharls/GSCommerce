
using GSCommerce.Client.Models;

namespace GSCommerceAPI.Models.SUNAT.DTOs
{
    public class ArqueoCajaDTO
    {
        public int IdAperturaCierre { get; set; }
        public DateOnly Fecha { get; set; }
        public string Usuario { get; set; } = "";
        public string Cajero { get; set; } = "";
        public decimal SaldoInicial { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Egresos { get; set; }
        public decimal VentaDia { get; set; }
        public decimal SaldoFinal { get; set; }
        public string? ObservacionCierre { get; set; }
        public List<ResumenCierreDeCaja> Resumen { get; set; } = new();
    }
}
