namespace GSCommerce.Client.Models.SUNAT
{
    public class CuotaDTO
    {
        public string Id { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
    }
}
