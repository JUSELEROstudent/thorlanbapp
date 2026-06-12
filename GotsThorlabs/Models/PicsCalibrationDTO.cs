namespace GotsThorlabs.Models
{
    public class PicsCalibrationDTO
    {
        public Guid PicsCalibrationId { get; set; }
        public Guid GroupCailbrationId { get; set; }
        public string Pic1 { get; set; } = default!;
        public string Pic2 { get; set; } = default!;
        public string AxeDirectionCalibration { get; set; } = default!;
        public bool Acepted { get; set; }
        public decimal dx { get; set; }
        public decimal dy { get; set; }
        public decimal Confidence { get; set; }
        public string MeasureUnit { get; set; } = default!;
        public decimal MovementValue { get; set; }
    }

    public class PicsCalibrationUpdateDTO
    {
        public Guid PicsCalibrationId { get; set; }
        public Guid GroupCailbrationId { get; set; }
        public string Pic1 { get; set; } = default!;
        public string Pic2 { get; set; } = default!;
        public string AxeDirectionCalibration { get; set; } = default!;
        public bool Acepted { get; set; }
        public decimal dx { get; set; }
        public decimal dy { get; set; }
        public decimal Confidence { get; set; }
        public string MeasureUnit { get; set; } = default!;
        public decimal MovementValue { get; set; }
    }
}
