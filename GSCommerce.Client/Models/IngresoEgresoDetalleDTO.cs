namespace GSCommerce.Client.Models;

public class IngresoEgresoDetalleDTO
{
    public string Forma { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string? Banco { get; set; }
    public string? Cuenta { get; set; }
    public string? ImagenBase64 { get; set; }
}