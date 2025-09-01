using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGradu.Models
{
    public class DetalleVenta
    {
        [Key]
        public int Id { get; set; }
        public int VentaId { get; set; } // Referencia a la venta
        public int Fk_Producto { get; set; }



        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal SubTotal { get; set; }

        [NotMapped] // Este atributo indica que no se mapeará a la base de datos
        public string? CodigoProducto { get; set; } // Se usa solo temporalmente para obtener el ID
        public virtual Venta? Venta { get; set; } // Relación con la tabla Ventas
        public virtual Producto Producto { get; set; }
    }
}

