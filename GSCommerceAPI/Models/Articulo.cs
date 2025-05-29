using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class Articulo
{
    public string IdArticulo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string DescripcionCorta { get; set; } = null!;

    public string Familia { get; set; } = null!;

    public string Linea { get; set; } = null!;

    public string Marca { get; set; } = null!;

    public string Material { get; set; } = null!;

    public string Modelo { get; set; } = null!;

    public string Color { get; set; } = null!;

    public string Detalle { get; set; } = null!;

    public string Talla { get; set; } = null!;

    public int IdProveedor { get; set; }

    public string UnidadAlmacen { get; set; } = null!;

    public string MonedaCosteo { get; set; } = null!;

    public decimal PrecioCompra { get; set; }

    public decimal PrecioVenta { get; set; }

    public DateOnly FechaRegistro { get; set; }

    public byte[]? Foto { get; set; }

    public byte[]? CodigoBarra { get; set; }

    public bool Estado { get; set; }

    public string? Estacion { get; set; }

    public virtual ICollection<ArticuloVariante> ArticuloVariantes { get; set; } = new List<ArticuloVariante>();

    public virtual ICollection<ComprobanteDeVentaDetalle> ComprobanteDeVentaDetalles { get; set; } = new List<ComprobanteDeVentaDetalle>();

    public virtual Proveedor IdProveedorNavigation { get; set; } = null!;

    public virtual ICollection<Kardex> Kardices { get; set; } = new List<Kardex>();

    public virtual ICollection<MovimientosDetalle> MovimientosDetalles { get; set; } = new List<MovimientosDetalle>();

    public virtual ICollection<OrdenDeCompraDetalle> OrdenDeCompraDetalles { get; set; } = new List<OrdenDeCompraDetalle>();

    public virtual ICollection<StockAlmacen> StockAlmacens { get; set; } = new List<StockAlmacen>();

    public virtual ICollection<TomaInventarioDetalle> TomaInventarioDetalles { get; set; } = new List<TomaInventarioDetalle>();
}
