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
    }
}
