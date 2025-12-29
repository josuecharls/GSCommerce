namespace GSCommerceAPI.Models;

/// <summary>
/// Representa una dirección asociada a un RUC.
/// Permite almacenar múltiples direcciones para cada RUC para uso en Guías de Remisión.
/// </summary>
public class DireccionRuc
{
    public int IdDireccionRuc { get; set; }
    
    /// <summary>
    /// RUC al que pertenece esta dirección (11 caracteres)
    /// </summary>
    public string Ruc { get; set; } = string.Empty;
    
    /// <summary>
    /// Razón social del RUC (para referencia)
    /// </summary>
    public string? RazonSocial { get; set; }
    
    /// <summary>
    /// Dirección completa
    /// </summary>
    public string Direccion { get; set; } = string.Empty;
    
    /// <summary>
    /// Código de ubigeo (6 caracteres)
    /// </summary>
    public string Ubigeo { get; set; } = string.Empty;
    
    public string? Departamento { get; set; }
    public string? Provincia { get; set; }
    public string? Distrito { get; set; }
    
    /// <summary>
    /// Nombre corto para identificar (ej: "Almacén Central", "Sucursal Lima")
    /// </summary>
    public string? Alias { get; set; }
    
    /// <summary>
    /// Indica si es la dirección predeterminada para este RUC
    /// </summary>
    public bool EsDefault { get; set; }
    
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}
