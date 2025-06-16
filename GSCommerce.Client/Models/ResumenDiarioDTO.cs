namespace GSCommerce.Client.Models
{
    public class ResumenDiarioDTO
    {
        public decimal Efectivo { get; set; }
        public decimal Tarjeta { get; set; }
        public decimal NotaCredito { get; set; }
        public decimal Total => Efectivo + Tarjeta + NotaCredito;
        public decimal SaldoInicial { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Egresos { get; set; }
        public decimal SaldoActual => SaldoInicial + Efectivo + Ingresos - Egresos;
    }
}
