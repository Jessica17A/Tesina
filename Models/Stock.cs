using System.ComponentModel.DataAnnotations;
using WebGradu.Models;

namespace WebGradu.Models
{
    public class Stock
    {
        public int Id { get; set; } // ID del stock, clave primaria

        [Required]
        public int Fk_Producto { get; set; } // ID del producto asociado

        [Range(0, int.MaxValue, ErrorMessage = "El stock inicial debe ser un valor positivo.")]
        public int? StockInicial { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock actual debe ser un valor positivo.")]
        public int? StockActual { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo debe ser un valor positivo.")]
        public int? StockMinimo { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock máximo debe ser un valor positivo.")]
        public int StockMaximo { get; set; } // Cambiado a no nullable

        public string TipoMovimiento { get; set; } // Tipo de movimiento

        public DateTime? FechaMovimiento { get; set; }

        // Propiedad de navegación
        public virtual Producto Producto { get; set; }
    }
}