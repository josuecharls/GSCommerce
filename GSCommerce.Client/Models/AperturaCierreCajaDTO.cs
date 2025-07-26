
namespace GSCommerce.Client.Models
{
    public class AperturaCierreCajaDTO
    {
        public int IdAperturaCierre { get; set; }

        public int IdUsuario { get; set; }

        public int IdAlmacen { get; set; }

        public DateOnly Fecha { get; set; }

        public decimal FondoFijo { get; set; }

        public decimal SaldoInicial { get; set; }

        public decimal VentaDia { get; set; }

        public decimal Ingresos { get; set; }

        public decimal Egresos { get; set; }

        public decimal SaldoFinal { get; set; }

        public string Estado { get; set; } = null!;

        public string? ObservacionApertura { get; set; }

        public string? ObservacionCierre { get; set; }

        // Agrega estos campos adicionales para evitar navegar objetos
        public string? NombreUsuario { get; set; }
        public string? NombreCajero { get; set; }
    }
}
