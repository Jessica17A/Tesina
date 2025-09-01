using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using WebGradu.Data;
using WebGradu.Models;

namespace WebGradu.Controllers
{
    [Authorize]
    public class CajaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CajaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Acción para mostrar el formulario y el historial de cuadres
        public IActionResult Index()
        {
            // Obtén el UserName del usuario logueado
            var userName = User.Identity.Name;
            var isAdmin = User.IsInRole("Admin");

            // Consulta los cuadres con estado 1
            var cuadresQuery = _context.Caja
                .Where(c => c.Estado == 1) // Solo registros con estado 1
                .AsQueryable();

            // Si el usuario no es Admin, filtra por su UserName
            if (!isAdmin)
            {
                cuadresQuery = cuadresQuery.Where(c => c.UserName == userName); // Filtra por UserName
            }

            // Ordena los cuadres y convierte a lista
            var cuadres = cuadresQuery
                .OrderByDescending(c => c.FechaCuadre)
                .ToList();

            return View(cuadres);
        }


        // Acción para verificar si ya existe un cuadre en las fechas seleccionadas
        [HttpPost]
        public JsonResult VerificarCuadre(DateTime fechaInicio, DateTime? fechaFin)
        {
            bool exists = false;

            if (fechaFin.HasValue && fechaFin.Value.Date >= fechaInicio.Date)
            {
                // Cuadre por rango: verificar si alguna fecha dentro del rango ya tiene un cuadre
                exists = _context.Caja.Any(c =>
                    c.TipoCuadre.Contains("Rango") &&
                    c.FechaCuadre >= fechaInicio.Date && c.FechaCuadre <= fechaFin.Value.Date);
            }
            else
            {
                // Cuadre diario: verificar si ya existe un cuadre para esa fecha
                exists = _context.Caja.Any(c =>
                    c.TipoCuadre.StartsWith("Diario") &&
                    c.FechaCuadre.Date == fechaInicio.Date);
            }

            return Json(exists);
        }

        // Acción para realizar el cuadre (diario o por rango)
        [HttpPost]
        public IActionResult RealizarCuadre(DateTime fechaInicio, DateTime? fechaFin, decimal dineroEfectivo)
        {
            bool isRange = fechaFin.HasValue && fechaFin.Value.Date >= fechaInicio.Date;

            // Validar si ya existe un cuadre para las fechas seleccionadas
            bool exists = false;

            if (isRange)
            {
                exists = _context.Caja.Any(c =>
                    c.TipoCuadre.Contains("Rango") &&
                    c.FechaCuadre >= fechaInicio.Date && c.FechaCuadre <= fechaFin.Value.Date);
            }
            else
            {
                exists = _context.Caja.Any(c =>
                    c.TipoCuadre.StartsWith("Diario") &&
                    c.FechaCuadre.Date == fechaInicio.Date);
            }

            if (exists)
            {
                return Json(new { success = false, message = "Ya se ha realizado un cuadre para las fechas seleccionadas." });
            }

            // Obtener las ventas que no han sido cuadradas y tienen Estado == 1
            var ventas = isRange ?
                _context.Ventas
                    .Where(v => v.Fecha.Date >= fechaInicio.Date && v.Fecha.Date <= fechaFin.Value.Date && v.Estado == 1 && !v.Cuadrada)
                    .ToList() :
                _context.Ventas
                    .Where(v => v.Fecha.Date == fechaInicio.Date && v.Estado == 1 && !v.Cuadrada)
                    .ToList();

            if (ventas.Count == 0)
            {
                return Json(new { success = false, message = "No hay ventas para cuadrar en las fechas seleccionadas." });
            }

            // Calcular el total de ventas
            var totalVentas = ventas.Sum(v => v.Total) ?? 0m;

            // Marcar las ventas como cuadradas
            foreach (var venta in ventas)
            {
                venta.Cuadrada = true;
            }

            // Crear un nuevo registro en Caja
            var cuadre = new Caja
            {
                FechaCuadre = DateTime.Now, // Fecha actual al realizar el cuadre
                TotalVentas = totalVentas,
                DineroEfectivo = dineroEfectivo,
                TipoCuadre = isRange
                    ? $"Rango ({fechaInicio.ToString("dd/MM/yyyy")} - {fechaFin.Value.ToString("dd/MM/yyyy")})"
                    : $"Diario ({fechaInicio.ToString("dd/MM/yyyy")})",
                UserName = User.Identity.Name, // Guardar el nombre del usuario autenticado
                Estado = 1 // Establecer estado a 1 al realizar el cuadre
            };

            _context.Caja.Add(cuadre);
            _context.SaveChanges();

            return Json(new { success = true, message = "Cuadre realizado exitosamente." });
        }


        // Acción para actualizar el dinero efectivo de un cuadre
        [HttpPost]
        public IActionResult UpdateCuadre(int id, decimal dineroEfectivo)
        {
            // Obtener el registro de la caja a actualizar
            var cuadre = _context.Caja.FirstOrDefault(c => c.Id == id);
            if (cuadre == null)
            {
                return Json(new { success = false, message = "Cuadre no encontrado." }); // Cambiar a success = false
            }

            // Actualizar el dinero en efectivo
            cuadre.DineroEfectivo = dineroEfectivo;
            _context.SaveChanges();

            return Json(new { success = true, message = "Cuadre actualizado exitosamente." }); 
        }


        // Acción para borrar un cuadre (actualiza el estado a 0)
        [HttpPost]
        public IActionResult BorrarCuadre(int id)
        {
            var cuadre = _context.Caja.FirstOrDefault(c => c.Id == id);
            if (cuadre == null)
            {
                return Json(new { success = false });
            }

            // Actualiza el estado a 0
            cuadre.Estado = 0;
            _context.SaveChanges();

            return Json(new { success = true });
        }
    }
}
