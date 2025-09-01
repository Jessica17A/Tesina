using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebGradu.Data;
using WebGradu.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace WebGradu.Controllers
{
    [Authorize]
    public class CatalogoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CatalogoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Catalogo/Index
        public async Task<IActionResult> Index(string searchQuery)
        {
            // Obtener los productos y sus categorías
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .AsNoTracking() // No realizar un seguimiento de los cambios
                .ToListAsync();

            // Aplicar filtrado en el lado del cliente
            if (!string.IsNullOrEmpty(searchQuery))
            {
                productos = productos.Where(p => p.Nombre.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                                 p.Descripcion.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                                     .ToList();
            }

            // Agrupar los productos por categoría
            var productosPorCategoria = productos.GroupBy(p => p.Categoria.NombreCategoria);

            // Pasar el término de búsqueda a la vista
            ViewData["SearchQuery"] = searchQuery;

            return View(productosPorCategoria);
        }
    }
}
