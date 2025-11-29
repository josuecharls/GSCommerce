namespace GSCommerce.Client.Models
{
    public class CrearMetaMensualRequest
    {
        public int IdAlmacen { get; set; }
        public int Anio { get; set; }
        public int Mes { get; set; }
        public int MesesHistoricos { get; set; } = 3; // Meses a considerar para cálculo histórico
        // MetaMensual ya no se envía, se calcula automáticamente
    }
}



