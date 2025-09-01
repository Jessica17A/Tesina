using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using WebGradu.Data;
using WebGradu.Models;

namespace WebGradu.Controllers
{
    [Authorize]
    public class VentasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VentasController(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index(string filtro, DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                // Obtener UserName del usuario logueado
                var userName = User.Identity.Name; // Nombre del usuario logueado
                var isAdmin = User.IsInRole("Admin");

                // Consulta las ventas activas
                var ventas = await _context.Ventas
                    .Where(v => v.Estado == 1) // Filtra solo ventas activas
                    .Include(v => v.DetalleVentas)
                    .ThenInclude(d => d.Producto)
                    .ToListAsync();

                // Si el usuario no es Admin, filtra por su UserName
                if (!isAdmin)
                {
                    ventas = ventas.Where(v => v.UsuarioId == userName).ToList();
                }

                // Filtrar según el parámetro
                if (filtro == "hoy")
                {
                    ventas = ventas.Where(v => v.Fecha.Date == DateTime.Today).ToList();
                }
                else if (filtro == "ayer")
                {
                    ventas = ventas.Where(v => v.Fecha.Date == DateTime.Today.AddDays(-1)).ToList();
                }
                else if (filtro == "ultimos7")
                {
                    ventas = ventas.Where(v => v.Fecha.Date >= DateTime.Today.AddDays(-7)).ToList();
                }
                else if (filtro == "ultimoMes")
                {
                    ventas = ventas.Where(v => v.Fecha.Date >= DateTime.Today.AddMonths(-1)).ToList();
                }
                else if (filtro == "rango" && fechaInicio.HasValue && fechaFin.HasValue)
                {
                    ventas = ventas.Where(v => v.Fecha.Date >= fechaInicio.Value.Date && v.Fecha.Date <= fechaFin.Value.Date).ToList();
                }

                return View(ventas);
            }
            catch (Exception ex)
            {
      
                return View(new List<Venta>()); // Retorna una lista vacía en caso de error
            }
        }




        public async Task<IActionResult> Detalles(int id)
        {
            // Buscar la venta por ID y cargar sus detalles
            var venta = await _context.Ventas
                .Include(v => v.DetalleVentas)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null)
            {
                return NotFound();
            }

            return PartialView("_Detalles", venta); 
        }




        // GET: Ventas/Create
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Crear(Venta venta, string detalles)
        {
            if (ModelState.IsValid)
            {
                // Iniciar una transacción de base de datos
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Asignar el nombre de usuario autenticado y la fecha
                        venta.UsuarioId = User.Identity.Name;
                        venta.Fecha = DateTime.Now;
                        venta.Estado = 1;

                        // Guardar la venta en la base de datos (sin confirmar aún)
                        _context.Ventas.Add(venta);
                        await _context.SaveChangesAsync(); // Obtener el ID de la venta

                        // Deserializar los detalles de la venta desde JSON
                        var listaDetalles = JsonConvert.DeserializeObject<List<DetalleVenta>>(detalles);

                        // Iterar sobre cada detalle
                        foreach (var detalle in listaDetalles)
                        {
                            // Verificar si el producto existe
                            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Codigo_Producto == detalle.CodigoProducto);
                            if (producto == null)
                            {
                                TempData["Error"] = $"Error: No se encontró el producto con el código: {detalle.CodigoProducto}.";
                                return View(venta);
                            }

                            // Verificar stock disponible
                            var stock = await _context.Stocks
                                .Where(s => s.Fk_Producto == producto.ProductoID)
                                .OrderByDescending(s => s.FechaMovimiento)
                                .FirstOrDefaultAsync();

                            // Verificar si el producto tiene stock asignado
                            if (stock == null)
                            {
                                TempData["Error"] = $"Error: Este Producto no tiene stock asignado.";
                                return View(venta);
                            }

                            // Verificar si hay suficiente stock
                            if (stock.StockActual < detalle.Cantidad)
                            {
                                TempData["Error"] = $"Error: Stock insuficiente para el producto. Unidades disponibles: {stock.StockActual}.";
                                return View(venta);
                            }

                            // Asignar IDs al detalle
                            detalle.Fk_Producto = producto.ProductoID;
                            detalle.VentaId = venta.Id;

                            // Agregar el detalle a la base de datos
                            _context.DetalleVentas.Add(detalle);

                            // Actualizar el stock
                            stock.StockActual -= detalle.Cantidad; // Reducir stock

                            // Crear un nuevo registro de movimiento en la tabla de Stock
                            var nuevoStockMovimiento = new Stock
                            {
                                Fk_Producto = producto.ProductoID, // Relacionar con el producto
                                StockInicial = stock.StockInicial, // Asignar el stock actual del último movimiento como inicial
                                StockActual = stock.StockActual, // Actualizar el stock actual después de la venta
                                StockMinimo = stock.StockMinimo, // Mantener el stock mínimo
                                StockMaximo = stock.StockMaximo, // Mantener el stock máximo
                                TipoMovimiento = "Venta", // Registrar como movimiento de venta
                                FechaMovimiento = DateTime.Now, // Asignar la fecha del movimiento
                            };

                            // Agregar el nuevo movimiento de stock a la base de datos
                            _context.Stocks.Add(nuevoStockMovimiento);
                            _context.Stocks.Update(stock); // Actualizar stock existente
                        }

                        // Guardar todos los cambios
                        await _context.SaveChangesAsync();

                        // Confirmar la transacción
                        await transaction.CommitAsync();

                        // Mensaje de éxito
                        TempData["VentaCreada"] = "La venta y los detalles se han registrado exitosamente.";
                        return RedirectToAction("Crear");
                    }
                    catch (Exception ex)
                    {
                        // Rollback si hay un error
                        await transaction.RollbackAsync();
                        TempData["Error"] = "Error al registrar la venta: " + ex.Message;
                    }
                }
            }

            // Si hay errores, volver a la vista de creación
            return View(venta);
        }


        [HttpGet]
        public IActionResult GenerarReporte(DateTime fechaInicio, DateTime fechaFin)
        {
            // Filtra las ventas en base a las fechas seleccionadas
            var ventas = _context.Ventas
                .Where(v => v.Fecha >= fechaInicio && v.Fecha <= fechaFin)
                .ToList();

            // Si no hay ventas en el rango de fechas, regresa al formulario
            if (!ventas.Any())
            {
                ModelState.AddModelError(string.Empty, "No se encontraron ventas en este rango de fechas.");
                return View("Index");
            }

            // Muestra la vista del reporte con las ventas filtradas
            return View("Reporte", ventas);
        }





        // Método para obtener el precio por código de producto
        [HttpGet]
        public JsonResult GetPrecioPorCodigo(string codigo)
        {
            var producto = _context.Productos.FirstOrDefault(p => p.Codigo_Producto == codigo);
            if (producto != null)
            {
                return Json(new { precio = producto.Precio_Venta });
            }
            return Json(new { precio = 0 });
        }

       
    }
}
