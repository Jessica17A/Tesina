using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using WebGradu.Data;
using WebGradu.Models;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Layout.Borders;
using Microsoft.EntityFrameworkCore;

public class ReportesPdfController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportesPdfController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult GenerarCuadres()
    {
        return View();
    }

    public IActionResult GenerarPDF(DateTime fechaInicio, DateTime? fechaFin)
    {
        var userName = User.Identity.Name;
        var isAdmin = User.IsInRole("Admin");

        // Filtrar los registros de cuadres según las fechas y el usuario
        var cuadresQuery = _context.Caja
            .Where(c => c.FechaCuadre >= fechaInicio && c.Estado == 1);

        // Si se proporciona una fecha de fin, se aplica el filtro adicional
        if (fechaFin.HasValue)
        {
            var fechaFinConHora = fechaFin.Value.AddDays(1).Date;
            cuadresQuery = cuadresQuery.Where(c => c.FechaCuadre < fechaFinConHora);
        }

        // Si el usuario no es admin, filtrar por su nombre de usuario
        if (!isAdmin)
        {
            cuadresQuery = cuadresQuery.Where(c => c.UserName == userName);
        }

        var cuadres = cuadresQuery.ToList();

        // Verificar si hay datos
        if (!cuadres.Any())
        {
            TempData["Mensaje"] = "No hay datos con la fecha seleccionada.";
            return RedirectToAction("GenerarCuadres");
        }

        // Generar el PDF
        using (var memoryStream = new MemoryStream())
        {
            var writer = new PdfWriter(memoryStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            // Obtener la ruta del archivo del logo
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logito.png");
            var logoImage = ImageDataFactory.Create(logoPath);
            var logo = new Image(logoImage).SetWidth(100).SetMarginBottom(10);
            document.Add(logo);

            // Encabezado de la tienda, movido más hacia arriba
            document.Add(new Paragraph("TIENDA YEILAS")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(18)
                .SetBold()
                .SetMarginTop(-50));
            document.Add(new Paragraph("Tel. 4965 3899")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(12)
                .SetMarginTop(0)
                .SetMarginBottom(5));
            document.Add(new Paragraph("\n"));

            document.Add(new Paragraph($"Fecha de Inicio: {fechaInicio:dd/MM/yyyy}")
                .SetTextAlignment(TextAlignment.LEFT)
                .SetFontSize(12));

            if (fechaFin.HasValue)
            {
                document.Add(new Paragraph($"Fecha de Fin: {fechaFin:dd/MM/yyyy}")
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetFontSize(12));
            }

            // Tabla de cuadres
            var table = new Table(new float[] { 2, 2, 2, 2, 2, 2 });
            table.SetWidth(UnitValue.CreatePercentValue(100));

            table.AddHeaderCell("Fecha Cuadre");
            table.AddHeaderCell("Total Ventas");
            table.AddHeaderCell("Tipo Cuadre");
            table.AddHeaderCell("Usuario");
            table.AddHeaderCell("Dinero Efectivo");
            table.AddHeaderCell("Estado Cuadre");

            foreach (var cuadre in cuadres)
            {
                table.AddCell(cuadre.FechaCuadre.ToString("dd/MM/yyyy"));
                table.AddCell(cuadre.TotalVentas.ToString("C"));
                table.AddCell(cuadre.TipoCuadre);
                table.AddCell(cuadre.UserName);
                table.AddCell(cuadre.DineroEfectivo.ToString("C"));

                var estadoCuadre = cuadre.TotalVentas == cuadre.DineroEfectivo ? "Cuadró" : "No Cuadró";
                table.AddCell(estadoCuadre);
            }

            // Aplicar estilo al borde izquierdo de la tabla
            table.SetBorder(Border.NO_BORDER);
            document.SetBorderLeft(new SolidBorder(new DeviceRgb(0, 51, 102), 4));
            document.Add(table);

            // Añadir un pie de página con el nombre de usuario
            document.Add(new Paragraph($"\nGenerado por: {userName}")
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetFontSize(12));

            document.Close();

            // Regresar el archivo PDF como respuesta
            return File(memoryStream.ToArray(), "application/pdf", $"Cuadres_{fechaInicio:yyyyMMdd}.pdf");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GenerarPDFVentas(DateTime fechaInicio, DateTime? fechaFin)
    {
        // Obtener las ventas dentro del rango de fechas con estado activo
        var ventasQuery = _context.Ventas
            .Include(v => v.DetalleVentas)
                .ThenInclude(d => d.Producto)
            .Where(v => v.Fecha.Date >= fechaInicio.Date && v.Estado == 1);

        // Si se proporciona una fecha de fin, se aplica el filtro adicional
        if (fechaFin.HasValue)
        {
            ventasQuery = ventasQuery.Where(v => v.Fecha.Date <= fechaFin.Value.Date);
        }

        var ventas = await ventasQuery.ToListAsync();

        // Verificar si hay datos
        if (!ventas.Any())
        {
            TempData["Mensaje"] = "No hay datos con la fecha seleccionada.";
            return RedirectToAction("GenerarCuadres");
        }

        // Crear un documento PDF
        using (var memoryStream = new MemoryStream())
        {
            using (var pdfWriter = new PdfWriter(memoryStream))
            {
                using (var pdfDocument = new PdfDocument(pdfWriter))
                {
                    var document = new Document(pdfDocument);

                    // Obtener la ruta del archivo del logo
                    var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logito.png");
                    var logoImage = ImageDataFactory.Create(logoPath);
                    var logo = new Image(logoImage).SetWidth(100).SetMarginBottom(10);
                    document.Add(logo);

                    // Encabezado de la tienda
                    document.Add(new Paragraph("TIENDA YEILAS")
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(18)
                        .SetBold()
                        .SetMarginTop(-0));
                    document.Add(new Paragraph("Tel. 4965 3899")
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(12)
                        .SetMarginTop(0)
                        .SetMarginBottom(5));

                    // Título alineado a la izquierda
                    document.Add(new Paragraph("Reporte de Ventas")
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(18)
                        .SetBold()
                        .SetMarginTop(10));

                    // Añadir la fecha de inicio y fin
                    document.Add(new Paragraph($"Desde: {fechaInicio:dd/MM/yyyy} Hasta: {fechaFin:dd/MM/yyyy}")
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(12));

                    decimal totalGeneral = 0;

                    // Agrupar ventas por VentaId
                    foreach (var venta in ventas)
                    {
                        // Título con el ID de la venta y la fecha
                        document.Add(new Paragraph($"Venta ID: {venta.Id} - Fecha: {venta.Fecha:dd/MM/yyyy}")
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetFontSize(16)
                            .SetBold());

                        // Crear tabla para los detalles de la venta actual
                        var tabla = new Table(6);
                        tabla.SetWidth(UnitValue.CreatePercentValue(100));
                        tabla.AddHeaderCell("Usuario");
                        tabla.AddHeaderCell("Producto");
                        tabla.AddHeaderCell("Código Producto");
                        tabla.AddHeaderCell("Cantidad");
                        tabla.AddHeaderCell("Precio Unitario");
                        tabla.AddHeaderCell("Precio Total");

                        decimal totalVenta = 0; // Total por venta
                        bool usuarioColocado = false; // Controlar que el Usuario solo se agregue en la primera fila

                        // Agregar detalles de la venta a la tabla
                        foreach (var detalle in venta.DetalleVentas)
                        {
                            // Llenar solo la primera celda de "Usuario" con el ID del usuario
                            tabla.AddCell(usuarioColocado ? "" : venta.UsuarioId.ToString());
                            usuarioColocado = true; // Cambiar a true para que las siguientes celdas queden en blanco

                            // Llenar el resto de los datos
                            tabla.AddCell(detalle.Producto.Nombre);
                            tabla.AddCell(detalle.Producto.Codigo_Producto);
                            tabla.AddCell(detalle.Cantidad.ToString());
                            tabla.AddCell(detalle.PrecioUnitario.ToString("C"));
                            tabla.AddCell(detalle.SubTotal.ToString("C"));

                            totalVenta += detalle.SubTotal; // Sumar al total de la venta
                        }

                        totalGeneral += totalVenta; // Sumar al total general

                        // Añadir la tabla de detalles al documento
                        document.Add(tabla);
                        document.Add(new Paragraph($"Total Venta: {totalVenta:C}")
                            .SetTextAlignment(TextAlignment.RIGHT)
                            .SetFontSize(12));
                    }

                    // Añadir el total general al final
                    document.Add(new Paragraph($"Total General: {totalGeneral:C}")
                        .SetTextAlignment(TextAlignment.RIGHT)
                        .SetFontSize(12)
                        .SetBold());

                    // Añadir pie de página con el nombre de usuario
                    var userName = User.Identity.Name;
                    document.Add(new Paragraph($"\nGenerado por: {userName}")
                        .SetTextAlignment(TextAlignment.RIGHT)
                        .SetFontSize(12));

                    document.Close();
                }
            }

            return File(memoryStream.ToArray(), "application/pdf", $"Ventas_{fechaInicio:yyyyMMdd}.pdf");
        }
    }
}
