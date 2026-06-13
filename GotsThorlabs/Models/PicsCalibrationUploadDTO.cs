using Microsoft.AspNetCore.Http;

namespace GotsThorlabs.Models
{
    public class PicsCalibrationUploadDTO
    {
        public string GroupCailbrationId { get; set; } = default!;
        public string AxeDirectionCalibration { get; set; } = default!;
        public bool Acepted { get; set; }
        public string Dx { get; set; } = default!;
        public string Dy { get; set; } = default!;
        public string Confidence { get; set; } = default!;
        public string MeasureUnit { get; set; } = default!;
        public string MovementValue { get; set; } = default!;
        public long? NumberOfSteps { get; set; }
        public string? AxisMovementName { get; set; }

        public IFormFile Pic1File { get; set; } = default!;
        public IFormFile Pic2File { get; set; } = default!;
    }
}
