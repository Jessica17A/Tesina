using System.ComponentModel.DataAnnotations;

namespace WebGradu.Models
{
    public class Producto
    {
        [Key]
        public int ProductoID { get; set; }
        public string? Codigo_Producto { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Precio_Compra { get; set; }
        public int? Fk_Categoria { get; set; }
        public string? Estado { get; set; }
        public string? Foto { get; set; }
        public decimal? Precio_Venta { get; set; }

        // Propiedad de navegación
        public virtual Categoria? Categoria { get; set; }
        public virtual Stock? Stock { get; set; }
    
    }
}

