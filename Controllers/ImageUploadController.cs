using Microsoft.AspNetCore.Mvc;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace WebGradu.Controllers
{
    public class ImageUploadController : Controller
    {
        private readonly Cloudinary _cloudinary;

        public ImageUploadController(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Route("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No se cargó el archivo.");
            }

            // Configurar los parámetros de carga
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = "Productos" // Usar 'Folder' en lugar de 'AssetFolder'
            };

            try
            {
                // Subir la imagen a Cloudinary
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    return BadRequest($"Error al cargar la imagen: {uploadResult.Error.Message}");
                }

                // Guardar la URL de la imagen en TempData para usarla en la vista
                TempData["imgUrl"] = uploadResult.SecureUrl.ToString();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al cargar la imagen: {ex.Message}");
            }
        }
    }
}
