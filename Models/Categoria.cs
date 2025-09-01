using System.ComponentModel.DataAnnotations;

namespace WebGradu.Models
{
    public class Categoria
    {
        public int CategoriaID { get; set; }

        [StringLength(255)]
        [Required]
        public string NombreCategoria { get; set; }

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public int Estado { get; set; }
    }
}