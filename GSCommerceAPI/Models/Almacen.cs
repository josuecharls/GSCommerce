using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class Almacen
{
    public int IdAlmacen { get; set; }

    public string Nombre { get; set; } = null!;

    public bool EsTienda { get; set; }

    public string Direccion { get; set; } = null!;

    public string? Dpd { get; set; }

    public string? Telefono { get; set; }

    public string? Celular { get; set; }

    public string? RazonSocial { get; set; }

    public string? Ruc { get; set; }

    public bool Estado { get; set; }

    public string? Ubigeo { get; set; }

    public string? Certificado { get; set; }

    public string? PasswordCertificado { get; set; }

    public string? Abreviacion { get; set; }

    public string?  UsuarioSol { get; set; }

    public string? ClaveSol { get; set; }

    public bool AfectoIgv { get; set; }

    public virtual ICollection<AlmacenCuentum> AlmacenCuenta { get; set; } = new List<AlmacenCuentum>();

    public virtual ICollection<AperturaCierreCaja> AperturaCierreCajas { get; set; } = new List<AperturaCierreCaja>();

    public virtual ICollection<IngresosEgresosCabecera> IngresosEgresosCabeceras { get; set; } = new List<IngresosEgresosCabecera>();

    public virtual ICollection<Kardex> Kardices { get; set; } = new List<Kardex>();

    public virtual ICollection<MovimientosCabecera> MovimientosCabeceras { get; set; } = new List<MovimientosCabecera>();

    public virtual ICollection<Personal> Personals { get; set; } = new List<Personal>();

    public virtual ICollection<ResumenCierreDeCaja> ResumenCierreDeCajas { get; set; } = new List<ResumenCierreDeCaja>();

    public virtual ICollection<SerieCorrelativo> SerieCorrelativos { get; set; } = new List<SerieCorrelativo>();

    public virtual ICollection<StockAlmacen> StockAlmacens { get; set; } = new List<StockAlmacen>();

    public virtual ICollection<TomaInventario> TomaInventarios { get; set; } = new List<TomaInventario>();
}
