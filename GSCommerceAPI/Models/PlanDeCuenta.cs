using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class PlanDeCuenta
{
    public string Cuenta { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public bool AfectoMovimiento { get; set; }

    public bool GeneraCuentasAutomaticas { get; set; }

    public bool RequiereCuentaAuxiliar { get; set; }

    public bool RegistraDocumentos { get; set; }

    public bool FiltroCajaEgresos { get; set; }

    public bool NotaCredito { get; set; }

    public bool NotaDebito { get; set; }

    public string AfectaResultado { get; set; } = null!;

    public string TipoCuenta { get; set; } = null!;

    public bool Estado { get; set; }
}
