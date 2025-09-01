using System;
using System.ComponentModel.DataAnnotations;

namespace WebGradu.Models
{
    public class Caja
    {
        [Key]
        public int Id { get; set; }
        public DateTime FechaCuadre { get; set; } // Fecha en que se realiza el cuadre
        public decimal TotalVentas { get; set; } // Total de ventas del cuadre
        public string TipoCuadre { get; set; } // Puede ser "Diario" o "Mensual"
        public string UserName { get; set; } // Nombre del usuario que realizó el cuadre
        public decimal DineroEfectivo { get; set; } // Monto de dinero efectivo ingresado por el vendedor

        public int Estado { get; set; } = 1; //Estado para activo o eliminacion


    }
}
