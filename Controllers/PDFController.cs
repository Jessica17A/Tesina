using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Image;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebGradu.Data;
using Microsoft.AspNetCore.Authorization;

namespace WebGradu.Controllers
{
    [Authorize]
    public class PDFController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PDFController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> GenerarPDF(DateTime? fechaInicio, DateTime? fechaFin)
        {
            // Consulta que replica la consulta SQL proporcionada
            var ventas = await (from v in _context.Ventas
                                join dv in _context.DetalleVentas on v.Id equals dv.VentaId
                                join p in _context.Productos on dv.Fk_Producto equals p.ProductoID
                                where (!fechaInicio.HasValue || v.Fecha >= fechaInicio.Value) &&
                                      (!fechaFin.HasValue || v.Fecha <= fechaFin.Value)
                                orderby v.Fecha
                                select new
                                {
                                    VentaId = v.Id,
                                    v.Fecha,
                                    v.Total,
                                    dv.Cantidad,
                                    p.Codigo_Producto,
                                    ProductoNombre = p.Nombre,
                                    PrecioUnitario = dv.PrecioUnitario,
                                    Subtotal = dv.SubTotal
                                }).ToListAsync();

            using (var memoryStream = new MemoryStream())
            {
                // Crear un documento PDF
                PdfWriter writer = new PdfWriter(memoryStream);
                PdfDocument pdf = new PdfDocument(writer);
                Document document = new Document(pdf);

                // Añadir logo al PDF
                string logoPath = "wwwroot/images/logito.png"; // Ruta al logo en tu proyecto
                if (System.IO.File.Exists(logoPath))
                {
                    Image logo = new Image(ImageDataFactory.Create(logoPath));
                    logo.SetHeight(50).SetWidth(100); // Ajustar el tamaño del logo
                    document.Add(logo);
                }

                // Añadir encabezado personalizado
                document.Add(new Paragraph("TIENDA YEILAS")
                    .SetBold().SetFontSize(24)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph("Reportes de Ventas")
                    .SetBold().SetFontSize(18)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20));

                document.Add(new Paragraph($"Fechas: {fechaInicio?.ToString("dd/MM/yyyy")} - {fechaFin?.ToString("dd/MM/yyyy")}")
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(10));

                // Crear tabla con encabezados de color rojo pastel
                Table table = new Table(new float[] { 2, 4, 2, 2, 2, 2, 2 });
                table.SetWidth(UnitValue.CreatePercentValue(100)); // Ocupa el 100% del ancho

                // Color rojo pastel para el encabezado
                Color redPastel = new DeviceRgb(255, 182, 193); // Definir color rojo pastel

                // Añadir encabezados de la tabla con color rojo pastel




                // Añadir encabezados de la tabla con color rojo pastel
                table.AddHeaderCell(new Cell().Add(new Paragraph("Código Producto"))
                    .SetBackgroundColor(redPastel).SetTextAlignment(TextAlignment.CENTER).SetBold());

                table.AddHeaderCell(new Cell().Add(new Paragraph("Producto"))
                    .SetBackgroundColor(redPastel).SetTextAlignment(TextAlignment.CENTER).SetBold());

                table.AddHeaderCell(new Cell().Add(new Paragraph("Cantidad"))
                    .SetBackgroundColor(redPastel).SetTextAlignment(TextAlignment.CENTER).SetBold());

                table.AddHeaderCell(new Cell().Add(new Paragraph("Precio Unitario"))
                    .SetBackgroundColor(redPastel).SetTextAlignment(TextAlignment.CENTER).SetBold());

                table.AddHeaderCell(new Cell().Add(new Paragraph("Subtotal"))
                    .SetBackgroundColor(redPastel).SetTextAlignment(TextAlignment.CENTER).SetBold());

                table.AddHeaderCell(new Cell().Add(new Paragraph("Total"))
                   .SetBackgroundColor(redPastel).SetTextAlignment(TextAlignment.CENTER).SetBold());

                // Variable para controlar cuándo se debe mostrar el total de una venta
                int ventaActualId = -1;

                // Rellenar la tabla con los datos de las ventas y sus productos
                foreach (var venta in ventas)
                {
                    table.AddCell(new Cell().Add(new Paragraph(venta.Codigo_Producto)).SetTextAlignment(TextAlignment.CENTER));
                    table.AddCell(new Cell().Add(new Paragraph(venta.ProductoNombre)).SetTextAlignment(TextAlignment.CENTER));
                    table.AddCell(new Cell().Add(new Paragraph(venta.Cantidad.ToString())).SetTextAlignment(TextAlignment.CENTER));
                    table.AddCell(new Cell().Add(new Paragraph($"{venta.PrecioUnitario:C}")).SetTextAlignment(TextAlignment.CENTER));
                    table.AddCell(new Cell().Add(new Paragraph($"{venta.Subtotal:C}")).SetTextAlignment(TextAlignment.CENTER));

                    // Solo mostrar el total si es la primera vez que se encuentra la venta actual
                    if (venta.VentaId != ventaActualId)
                    {
                        table.AddCell(new Cell().Add(new Paragraph($"{venta.Total:C}")).SetTextAlignment(TextAlignment.CENTER));
                        ventaActualId = venta.VentaId; // Actualizar el ID de la venta actual
                    }
                    else
                    {
                        // Dejar la celda vacía para las siguientes filas de la misma venta
                        table.AddCell(new Cell());
                    }
                }


                // Añadir la tabla al documento
                document.Add(table);

                // Cerrar el documento
                document.Close();

                // Retornar el PDF como descarga
                var fileBytes = memoryStream.ToArray();
                return File(fileBytes, "application/pdf", "ReporteVentas.pdf");
            }
        }
    }
}
