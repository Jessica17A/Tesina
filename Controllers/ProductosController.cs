using Microsoft.AspNetCore.Mvc;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using WebGradu.Data;
using WebGradu.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;



namespace WebGradu.Controllers
{
    [Authorize]
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary;

        public ProductosController(ApplicationDbContext context, Cloudinary cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        // Método para mostrar el formulario
        public IActionResult Crear()
        {
            // Obtener las categorías desde la base de datos y concatenar ID con Nombre
            ViewBag.Categorias = _context.Categorias
                .Where(c => c.Estado == 1) // Mostrar solo las categorías activas
                .Select(c => new
                {
                    c.CategoriaID,
                    NombreCategoria = c.CategoriaID + " - " + c.NombreCategoria
                })
                .ToList();

            return View();
        }

        
        [HttpPost]
        public async Task<IActionResult> Crear(Producto producto, IFormFile file)
        {
            // Generar un código único para el producto
            producto.Codigo_Producto = GenerarCodigoUnico();

            // Validar si el código ya existe en la base de datos
            while (_context.Productos.Any(p => p.Codigo_Producto == producto.Codigo_Producto))
            {
                producto.Codigo_Producto = GenerarCodigoUnico(); // Regenerar si hay duplicados
            }

            // Asigna el valor "1" al campo Estado
            producto.Estado = "1";

            if (file != null && file.Length > 0)
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    Folder = "Productos"
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    ModelState.AddModelError(string.Empty, "Error al subir la imagen.");
                    return View(producto);
                }

                // En lugar de almacenar la URL directamente, almacenamos el PublicID
                producto.Foto = uploadResult.PublicId;
            }

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            TempData["ProductoCreado"] = "true"; 

            return RedirectToAction("Crear");
        }

        // En tu controlador de Productos
        [HttpGet]
        public JsonResult GetPrecioPorCodigo(string codigo)
        {
            var producto = _context.Productos.FirstOrDefault(p => p.Codigo_Producto == codigo);
            if (producto != null)
            {
                return Json(new { Precio = producto.Precio_Venta });
            }
            return Json(new { Precio = 0 }); // Retorna 0 si no se encuentra el producto
        }

        //// Método para obtener el ID del producto basado en el código ingresado
        //[HttpGet]
        //public IActionResult GetProductoIdPorCodigo(string codigo)
        //{
        //    var producto = _context.Productos.FirstOrDefault(p => p.Codigo_Producto == codigo);
        //    if (producto != null)
        //    {
        //        return Json(new { productoId = producto.ProductoID });
        //    }
        //    return Json(new { productoId = (int?)null });
        //}

        // Método para generar el código único de 3 letras y 3 números
        private string GenerarCodigoUnico()
        {
            var letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var numeros = "0123456789";
            var random = new Random();

            // Generar 3 letras aleatorias
            var letrasAleatorias = new string(Enumerable.Repeat(letras, 3)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            // Generar 3 números aleatorios
            var numerosAleatorios = new string(Enumerable.Repeat(numeros, 3)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            // Combinar letras y números
            return letrasAleatorias + numerosAleatorios;
        }
    }
}
