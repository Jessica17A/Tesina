using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebGradu.Data;
using WebGradu.Models;


namespace Productos.Controllers
{
    [Authorize]
    public class CategoriasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categorias
        public async Task<IActionResult> Index(string searchString)
        {
            // Guarda el término de búsqueda en ViewData para que sea accesible en la vista
            ViewData["CurrentFilter"] = searchString;

            // Obtiene todas las categorías con Estado igual a "1"
            var categorias = from c in _context.Categorias
                             where c.Estado == 1 // Filtra solo categorías activas
                             select c;

            // Verifica si hay un término de búsqueda
            if (!String.IsNullOrEmpty(searchString))
            {
                categorias = categorias.Where(c => c.NombreCategoria.Contains(searchString)
                || c.Descripcion.Contains(searchString));
            }

            // Retorna las categorías (filtradas o no) a la vista
            return View(await categorias.ToListAsync());
        }

        // GET: Categorias/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(m => m.CategoriaID == id && m.Estado == 1);
            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        // GET: Categorias/Create
        public IActionResult Create()
        {
            return View();
        }



        // POST: Categorias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoriaID,NombreCategoria,Descripcion,FechaCreacion")] Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                // Asignar el valor 1 al campo Estado al crear la categoría
                categoria.Estado = 1; // O 1 si Estado es de tipo int

                _context.Add(categoria);
                await _context.SaveChangesAsync();
                TempData["CategoriaCreada"] = "La categoría ha sido creada exitosamente."; // Almacena el mensaje en TempData

                return RedirectToAction(nameof(Create)); // Redirige a la misma página para mostrar la alerta
            }
            return View(categoria);
        }



        // GET: Categorias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }
            return View(categoria);
        }

        // POST: Categorias/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoriaID,NombreCategoria,Descripcion,FechaCreacion,FechaModificacion")] Categoria categoria)
        {
            if (id != categoria.CategoriaID)
            {
                return NotFound();
            }

            // Recuperar la categoría existente desde la base de datos para mantener el estado original
            var categoriaExistente = await _context.Categorias.FindAsync(id);
            if (categoriaExistente == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Solo actualiza los campos permitidos y no el campo Estado
                    categoriaExistente.NombreCategoria = categoria.NombreCategoria;
                    categoriaExistente.Descripcion = categoria.Descripcion;
                    categoriaExistente.FechaCreacion = categoria.FechaCreacion;

                    // Estado no se actualiza aquí

                    _context.Update(categoriaExistente);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoriaExists(categoria.CategoriaID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(categoria);
        }

        // GET: Categorias/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(m => m.CategoriaID == id && m.Estado == 1);
            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        // POST: Categorias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria != null)
            {
                // Actualiza el campo Estado en lugar de eliminar el registro
                categoria.Estado = 0; // O el valor que desees para indicar que está eliminado
                _context.Update(categoria);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategoriaExists(int id)
        {
            return _context.Categorias.Any(e => e.CategoriaID == id);
        }
    }
}