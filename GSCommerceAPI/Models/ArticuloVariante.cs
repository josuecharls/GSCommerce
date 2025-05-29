using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GSCommerceAPI.Models;

public partial class ArticuloVariante
{
    public int IdVariante { get; set; }

    public string IdArticulo { get; set; } = null!;

    public string Color { get; set; } = null!;

    public string Talla { get; set; } = null!;

    [JsonIgnore]
    [BindNever]
    public virtual Articulo? IdArticuloNavigation { get; set; } = null!;
}
