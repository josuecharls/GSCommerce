using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class Log
{
    public int Id { get; set; }

    public DateTime? Fecha { get; set; }

    public string? Modulo { get; set; }

    public string? Error { get; set; }

    public string? Usuario { get; set; }
}
