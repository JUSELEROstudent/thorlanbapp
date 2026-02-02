using Microsoft.AspNetCore.Http;

namespace GotsThorlabs.Models
{
    // DTO para crear una calibración con imágenes enviadas como multipart/form-data
    public class PicsCalibrationUploadDTO
    {
        public Guid GroupCailbrationId { get; set; }
        public string AxeDirectionCalibration { get; set; } = default!; // length 2
        public bool Acepted { get; set; }
        public decimal dx { get; set; }
        public decimal dy { get; set; }
        public decimal Confidence { get; set; }
        public string MeasureUnit { get; set; } = default!;
        public decimal MovementValue { get; set; }

        // Archivos de imagen
        public IFormFile Pic1File { get; set; } = default!;
        public IFormFile Pic2File { get; set; } = default!;
    }
}
