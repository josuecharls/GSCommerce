using System.ComponentModel.DataAnnotations;

namespace GSCommerce.Client.Models;

public class SerieGuiaRemisionDTO
{
    public int IdSerieGuiaRemision { get; set; }

    [Required(ErrorMessage = "Seleccione un almacén")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un almacén")]
    public int IdAlmacen { get; set; }

    [Required(ErrorMessage = "Ingrese la serie")]
    [StringLength(4, ErrorMessage = "La serie debe tener máximo 4 caracteres")]
    public string Serie { get; set; } = string.Empty;

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Correlativo inválido")]
    public int Correlativo { get; set; }

    public bool Estado { get; set; } = true;

    public string? NombreAlmacen { get; set; }
}
