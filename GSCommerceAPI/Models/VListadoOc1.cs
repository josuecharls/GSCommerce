﻿using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VListadoOc1
{
    public int IdOc { get; set; }

    public int IdProveedor { get; set; }

    public string Nombre { get; set; } = null!;

    public string NumeroOc { get; set; } = null!;

    public DateTime FechaOc { get; set; }

    public DateTime FechaEntrega { get; set; }

    public decimal ImporteSubTotal { get; set; }

    public decimal ImporteIgv { get; set; }

    public decimal ImporteTotal { get; set; }

    public string? Estado { get; set; }

    public DateTime? FechaAtencionTotal { get; set; }

    public DateTime? FechaAnulado { get; set; }

    public string Glosa { get; set; } = null!;
}
