namespace GSCommerce.Client.Models
{
    public class MetaMensualDetalleDTO
    {
        public int IdMetaMensualDetalle { get; set; }
        public DateOnly Fecha { get; set; }
        public decimal MetaDiaria { get; set; }
        public decimal? VentaReal { get; set; }
        public decimal? PorcentajeCumplimiento { get; set; }
        public int DiaSemana { get; set; } // 0=Domingo, 6=Sábado
        public int NumeroSemana { get; set; }
        public int NumeroQuincena { get; set; } // 1 o 2
    }
}





