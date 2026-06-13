namespace GotsThorlabs.Models
{
    public class PicsCalibrationDTO
    {
        public string PicsCalibrationId { get; set; } = default!;
        public string GroupCailbrationId { get; set; } = default!;
        public string Pic1 { get; set; } = default!;
        public string Pic2 { get; set; } = default!;
        public string AxeDirectionCalibration { get; set; } = default!;
        public long Acepted { get; set; }
        public string Dx { get; set; } = default!;
        public string Dy { get; set; } = default!;
        public string Confidence { get; set; } = default!;
        public string MeasureUnit { get; set; } = default!;
        public string MovementValue { get; set; } = default!;
    }

    public class PicsCalibrationUpdateDTO
    {
        public string PicsCalibrationId { get; set; } = default!;
        public string GroupCailbrationId { get; set; } = default!;
        public string Pic1 { get; set; } = default!;
        public string Pic2 { get; set; } = default!;
        public string AxeDirectionCalibration { get; set; } = default!;
        public long Acepted { get; set; }
        public string Dx { get; set; } = default!;
        public string Dy { get; set; } = default!;
        public string Confidence { get; set; } = default!;
        public string MeasureUnit { get; set; } = default!;
        public string MovementValue { get; set; } = default!;
    }
}
