namespace GSCommerce.Client.Models.DTOs.Reportes;

public class MetaDiariaImportResultDTO
{
    public int Importadas { get; set; }
    public int Omitidas { get; set; }
    public List<string> Errores { get; set; } = new();
    public List<string> Advertencias { get; set; } = new();
}
