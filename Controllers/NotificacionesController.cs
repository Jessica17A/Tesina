using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebGradu.Models;
using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet;
using WebGradu.Data;
using Microsoft.AspNetCore.Authorization;

namespace WebGradu.Controllers
{
    [Authorize]
    public class NotificacionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary; // Añadir Cloudinary

        public NotificacionesController(ApplicationDbContext context, Cloudinary cloudinary) // Inyectar Cloudinary
        {
            _context = context;
            _cloudinary = cloudinary;
        }
    [HttpGet]
    public IActionResult Index()
    {
    // Obtener el último movimiento de cada producto
    var ultimosMovimientos = _context.Stocks
        .GroupBy(s => s.Fk_Producto)
        .Select(g => g.OrderByDescending(s => s.FechaMovimiento).FirstOrDefault())
        .ToList();

    // Filtrar solo aquellos movimientos que tienen stock bajo
    var productosConStockMinimo = ultimosMovimientos
        .Where(s => s.StockActual <= s.StockMinimo && s.StockActual > 0)
        .ToList();

    // Incluir la información del producto
    foreach (var stock in productosConStockMinimo)
    {
        stock.Producto = _context.Productos.FirstOrDefault(p => p.ProductoID == stock.Fk_Producto);
        if (stock.Producto != null)
        {
            stock.Producto.Foto = ObtenerUrlImagen(stock.Producto.Foto); // Generar URL desde Cloudinary
        }
    }

    return View(productosConStockMinimo); // Devolver la vista con los productos filtrados
    }




        // Método para generar la URL segura de la imagen usando el public_id
        private string ObtenerUrlImagen(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
            {
                return "/path/to/default/image.jpg"; // Devuelve una imagen predeterminada si no hay imagen
            }

            var url = _cloudinary.Api.UrlImgUp
                        .Secure(true) // Para generar una URL segura (https)
                        .BuildUrl(publicId);
            return url;
        }
    }
}
