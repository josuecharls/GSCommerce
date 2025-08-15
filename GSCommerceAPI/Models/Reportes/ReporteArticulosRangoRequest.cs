namespace GSCommerceAPI.Models.Reportes
{
    public class ReporteArticulosRangoRequest
    {
        public List<string> Ids { get; set; } = new();
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
    }
}
