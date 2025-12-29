
namespace GSCommerce.Client.Models
{
    public class DireccionRucDTO
    {
        public int IdDireccionRuc { get; set; }
        public string Ruc { get; set; } = string.Empty;
        public string? RazonSocial { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public string Ubigeo { get; set; } = string.Empty;
        public string? Departamento { get; set; }
        public string? Provincia { get; set; }
        public string? Distrito { get; set; }
        public string? Alias { get; set; }
        public bool EsDefault { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public string DescripcionCompleta => string.IsNullOrEmpty(Alias) 
            ? Direccion 
            : $"{Alias} - {Direccion}";
    }
}
