using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebGradu.Models
{
    public class Venta
    {
        [Key]
        public int Id { get; set; }
        public string? UsuarioId { get; set; } // Para almacenar el ID del usuario
        public DateTime Fecha { get; set; }
        public decimal? Total { get; set; }

        public int Estado {  get; set; }
        public bool Cuadrada { get; set; } = false;


        // Propiedad de navegación para los detalles de la venta
        public virtual ICollection<DetalleVenta> DetalleVentas { get; set; } = new List<DetalleVenta>();
      

    }
}
