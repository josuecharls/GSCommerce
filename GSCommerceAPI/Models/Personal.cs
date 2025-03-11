using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class Personal
{
    public int IdPersonal { get; set; }

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string DocIdentidad { get; set; } = null!;

    public string NumeroDocIdentidad { get; set; } = null!;

    public string? Ruc { get; set; }

    public string Sexo { get; set; } = null!;

    public DateTime? FechaNacimiento { get; set; }

    public string? Direccion { get; set; }

    public string? Telefono { get; set; }

    public string? Celular { get; set; }

    public string? Email { get; set; }

    public string? EstadoCivil { get; set; }

    public string TipoEmpleado { get; set; } = null!;

    public string? LugarNacimiento { get; set; }

    public string? Nacionalidad { get; set; }

    public string? GradoInstruccion { get; set; }

    public string? Especialidad { get; set; }

    public int? IdAlmacen { get; set; }

    public string? Cargo { get; set; }

    public DateTime? FechaIngreso { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public byte[]? Foto { get; set; }

    public bool Estado { get; set; }

    public virtual Almacen? IdAlmacenNavigation { get; set; }

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
