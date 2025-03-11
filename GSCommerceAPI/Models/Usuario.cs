using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string Nombre { get; set; } = null!;

    public int IdPersonal { get; set; }

    public bool Estado { get; set; }

    public byte[]? Clave { get; set; }

    public virtual ICollection<AperturaCierreCaja> AperturaCierreCajas { get; set; } = new List<AperturaCierreCaja>();

    public virtual Personal IdPersonalNavigation { get; set; } = null!;

    public virtual ICollection<IngresosEgresosCabecera> IngresosEgresosCabeceras { get; set; } = new List<IngresosEgresosCabecera>();

    public virtual ICollection<OrdenDeCompraCabecera> OrdenDeCompraCabeceras { get; set; } = new List<OrdenDeCompraCabecera>();

    public virtual ICollection<ResumenCierreDeCaja> ResumenCierreDeCajas { get; set; } = new List<ResumenCierreDeCaja>();
}
