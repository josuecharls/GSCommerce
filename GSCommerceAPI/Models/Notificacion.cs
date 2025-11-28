using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GSCommerceAPI.Models
{
    public partial class Notificacion
    {
        [Key]
        public int IdNotificacion { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [Required]
        public int IdMovimiento { get; set; }

        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } = "TRANSFERENCIA";

        [Required]
        [StringLength(500)]
        public string Mensaje { get; set; } = string.Empty;

        [Required]
        public bool Leida { get; set; } = false;

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaLectura { get; set; }

        [ForeignKey("IdUsuario")]
        public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

        [ForeignKey("IdMovimiento")]
        public virtual MovimientosCabecera IdMovimientoNavigation { get; set; } = null!;
    }
}

