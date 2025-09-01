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
    public class StockController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary; // Añadir Cloudinary

        public StockController(ApplicationDbContext context, Cloudinary cloudinary) // Inyectar Cloudinary
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        [HttpGet]
        public async Task<IActionResult> Buscar(string query)
        {
            // Verificar si hay un término de búsqueda
            if (string.IsNullOrEmpty(query))
            {
                // Si no hay consulta, redirigir a la acción Index
                return RedirectToAction(nameof(Index));
            }

            // Buscar productos que coincidan con el término de búsqueda
            var productos = await _context.Productos
                .Where(p => p.Nombre.Contains(query) || p.Codigo_Producto.Contains(query))
                .ToListAsync();

            // Obtener stock para cada producto
            var stockDictionary = await _context.Stocks
                .GroupBy(s => s.Fk_Producto)
                .Select(g => g.OrderByDescending(s => s.StockActual).FirstOrDefault())
                .ToDictionaryAsync(s => s.Fk_Producto);

            // Pasar los productos y el stock a la vista
            ViewData["ProductosSinStock"] = productos.Where(p => !stockDictionary.ContainsKey(p.ProductoID)).ToList();
            ViewData["ProductosConStock"] = productos.Where(p => stockDictionary.ContainsKey(p.ProductoID)).ToList();
            ViewData["StockDictionary"] = stockDictionary;

            return View("Index"); // Asegúrate de que tu vista se llama Index
        }

        // GET: Stock
        public async Task<IActionResult> Index()
        {
            // Obtener productos que no tienen stock registrado
            var productosSinStock = await _context.Productos
                .Where(p => !_context.Stocks.Any(s => s.Fk_Producto == p.ProductoID))
                .ToListAsync();

            // Obtener productos con stock registrado
            var productosConStock = await _context.Productos
                .Where(p => _context.Stocks.Any(s => s.Fk_Producto == p.ProductoID))
                .ToListAsync();

            // Generar URL de la imagen para productos sin stock
            foreach (var producto in productosSinStock)
            {
                producto.Foto = ObtenerUrlImagen(producto.Foto); // Genera la URL segura
            }

            // Generar URL de la imagen para productos con stock
            foreach (var producto in productosConStock)
            {
                producto.Foto = ObtenerUrlImagen(producto.Foto); // Genera la URL segura
            }

            // Obtener stock actual para cada producto evitando duplicados
            var stockDictionary = await _context.Stocks
                .GroupBy(s => s.Fk_Producto) // Agrupar por ProductoID
                .Select(g => g.OrderByDescending(s => s.FechaMovimiento) // Ordenar por fecha de movimiento
                              .FirstOrDefault()) // Obtener el más reciente
                .ToDictionaryAsync(s => s.Fk_Producto); // Crear un diccionario

            // Pasar datos a la vista
            ViewData["ProductosSinStock"] = productosSinStock;
            ViewData["ProductosConStock"] = productosConStock;
            ViewData["StockDictionary"] = stockDictionary;

            return View();
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
        [HttpPost]
        public IActionResult LlenarStock(int id, int nuevoStock)
        {
            // Busca el último stock existente para el producto
            var stock = _context.Stocks
                .Where(s => s.Fk_Producto == id)
                .OrderByDescending(s => s.FechaMovimiento) // Ordena por fecha de movimiento para obtener el más reciente
                .FirstOrDefault();

            if (stock != null)
            {
                // Suma el nuevo stock al existente
                var stockActualizado = stock.StockActual + nuevoStock;

                // Crea un nuevo registro de stock con el stock actualizado
                var nuevoRegistro = new Stock
                {
                    Fk_Producto = stock.Fk_Producto,
                    StockInicial = nuevoStock, // El nuevo stock que se está ingresando
                    StockActual = stockActualizado, // Suma el stock actual más el nuevo stock
                    StockMinimo = stock.StockMinimo, // Mantiene el stock mínimo del registro original
                    StockMaximo = stock.StockMaximo, // Mantiene el stock máximo del registro original
                    TipoMovimiento = "Reabastecimiento",
                    FechaMovimiento = DateTime.Now
                };

                // Agrega el nuevo registro al contexto
                _context.Stocks.Add(nuevoRegistro);
            }

            // Guarda los cambios en la base de datos
            _context.SaveChanges();
            TempData["ActualizacionExitosa"] = true; // Mensaje de éxito

            return RedirectToAction("Index"); // Redirige a la vista adecuada
        }





        [HttpPost]
        public async Task<IActionResult> ActualizarStock(int id, int nuevoStock)
        {
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Fk_Producto == id);

            if (stock == null)
            {
                stock = new Stock
                {
                    Fk_Producto = id,
                    StockInicial = nuevoStock,
                    StockActual = nuevoStock,
                    StockMinimo = 5, // Asigna un valor predeterminado
                    StockMaximo = 30, // Asigna un valor predeterminado
                    TipoMovimiento = "Inicial"
                };
                _context.Stocks.Add(stock);
            }
            else
            {
                stock.StockInicial = nuevoStock;
                stock.StockActual = nuevoStock;
                _context.Stocks.Update(stock);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}