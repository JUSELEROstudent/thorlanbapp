using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GotsThorlabs.BLL;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhaseCorrelationCalibrationController : ControllerBase
    {
        /// <summary>
        /// corelacion de desplazamientos las unidades resultado en que va dado en pixeles?
        /// </summary>
        /// <param name="img1">Direccion fisica de la imagen 1</param>
        /// <param name="img2">Direccion fisica de la imagen 2</param>
        [HttpPost]
        public IActionResult CreatePhaseCorrelationCalibration(string img1,string img2)
        {
            // 1. Cargar imágenes como Mat
            var mat1 = new OpenCvSharp.Mat(img1, OpenCvSharp.ImreadModes.Grayscale);
            var mat2 = new OpenCvSharp.Mat(img2, OpenCvSharp.ImreadModes.Grayscale);

            // 2. Convertir a float32 (CV_32FC1)
            var mat1f = new OpenCvSharp.Mat();
            var mat2f = new OpenCvSharp.Mat();
            mat1.ConvertTo(mat1f, OpenCvSharp.MatType.CV_32F);
            mat2.ConvertTo(mat2f, OpenCvSharp.MatType.CV_32F);

            // 3. Detectar desplazamiento
            var (shift, confidence) = GotsThorlabs.BLL.PhaseCorrelationDetector.DetectShiftSimple(mat1f, mat2f);

            // 4. Liberar recursos
            mat1.Dispose();
            mat2.Dispose();
            mat1f.Dispose();
            mat2f.Dispose();

            // 5. Devolver resultado
            return Ok(new { dx = shift.X, dy = shift.Y, confidence });
        }

        /// <summary>
        /// Recibe dos archivos de imagen (multipart/form-data) y calcula el desplazamiento por correlación de fase.
        /// </summary>
        /// <param name="img1">Archivo de imagen 1</param>
        /// <param name="img2">Archivo de imagen 2</param>
        [HttpPost("file-data")]
        [Consumes("multipart/form-data")]
        public IActionResult CreatePhaseCorrelationCalibrationFromFiles(IFormFile img1, IFormFile img2)
        {
            if (img1 == null || img1.Length == 0)
                return BadRequest("img1 no fue enviado o está vacío.");
            if (img2 == null || img2.Length == 0)
                return BadRequest("img2 no fue enviado o está vacío.");

            OpenCvSharp.Mat mat1 = null!;
            OpenCvSharp.Mat mat2 = null!;
            OpenCvSharp.Mat mat1Gray = null!;
            OpenCvSharp.Mat mat2Gray = null!;
            OpenCvSharp.Mat mat1f = null!;
            OpenCvSharp.Mat mat2f = null!;

            try
            {
                // Leer img1 en memoria y decodificar a Mat
                using (var ms1 = new System.IO.MemoryStream())
                {
                    img1.CopyTo(ms1);
                    var bytes1 = ms1.ToArray();
                    mat1 = OpenCvSharp.Cv2.ImDecode(bytes1, OpenCvSharp.ImreadModes.Color);
                }

                // Leer img2 en memoria y decodificar a Mat
                using (var ms2 = new System.IO.MemoryStream())
                {
                    img2.CopyTo(ms2);
                    var bytes2 = ms2.ToArray();
                    mat2 = OpenCvSharp.Cv2.ImDecode(bytes2, OpenCvSharp.ImreadModes.Color);
                }

                if (mat1.Empty() || mat2.Empty())
                    return BadRequest("No se pudo decodificar una o ambas imágenes.");

                // Convertir a escala de grises
                mat1Gray = new OpenCvSharp.Mat();
                mat2Gray = new OpenCvSharp.Mat();
                OpenCvSharp.Cv2.CvtColor(mat1, mat1Gray, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
                OpenCvSharp.Cv2.CvtColor(mat2, mat2Gray, OpenCvSharp.ColorConversionCodes.BGR2GRAY);

                // Convertir a float32 (CV_32F)
                mat1f = new OpenCvSharp.Mat();
                mat2f = new OpenCvSharp.Mat();
                mat1Gray.ConvertTo(mat1f, OpenCvSharp.MatType.CV_32F);
                mat2Gray.ConvertTo(mat2f, OpenCvSharp.MatType.CV_32F);

                // Detectar desplazamiento (misma lógica que el endpoint original)
                var (shift, confidence) = GotsThorlabs.BLL.PhaseCorrelationDetector.DetectShiftSimple(mat1f, mat2f);

                return Ok(new { dx = shift.X, dy = shift.Y, confidence });
            }
            catch (System.Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error procesando imágenes: {ex.Message}");
            }
            finally
            {
                // Liberar recursos
                mat1?.Dispose();
                mat2?.Dispose();
                mat1Gray?.Dispose();
                mat2Gray?.Dispose();
                mat1f?.Dispose();
                mat2f?.Dispose();
            }
        }
    }
}
