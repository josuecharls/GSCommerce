﻿using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VPersonal1
{
    public int IdPersonal { get; set; }

    public string Personal { get; set; } = null!;

    public string Local { get; set; } = null!;

    public string? Cargo { get; set; }

    public bool Estado { get; set; }
}
