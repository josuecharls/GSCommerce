using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

/// <summary>
/// Eventos CRUD
/// </summary>
public partial class Evento
{
    public int Id { get; set; }

    public DateTime? Fecha { get; set; }

    public string? Glosa { get; set; }

    public string? FilasAfectadas { get; set; }

    public string? Crud { get; set; }
}
