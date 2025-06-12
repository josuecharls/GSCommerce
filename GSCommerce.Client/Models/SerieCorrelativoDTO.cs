using System.ComponentModel.DataAnnotations;

namespace GSCommerce.Client.Models
{
    public class SerieCorrelativoDTO
    {
        public int IdSerieCorrelativo { get; set; }

        [Required(ErrorMessage = "Seleccione un almacén")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un almacén")]
        public int IdAlmacen { get; set; }

        [Required(ErrorMessage = "Seleccione tipo de documento")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione tipo de documento")]
        public int IdTipoDocumentoVenta { get; set; }

        [Required(ErrorMessage = "Ingrese la serie")]
        [StringLength(3, ErrorMessage = "La serie debe tener máximo 3 caracteres")]
        public string Serie { get; set; } = string.Empty;

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Correlativo inválido")]
        public int Correlativo { get; set; }

        public bool Estado { get; set; }

        public string? NombreAlmacen { get; set; }
        public string? NombreTipoDocumento { get; set; }
    }
}
