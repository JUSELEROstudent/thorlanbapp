namespace GotsThorlabs.Models
{
    public class TourDTO
    {
        public long IdTour { get; set; }
        public string Date { get; set; } = default!;
        public string NameFolder { get; set; } = default!;
        public long NumberX { get; set; }
        public long NumberY { get; set; }
        public long NumberZ { get; set; }
        public long Camera { get; set; }
        public string? EndStatus { get; set; }
        public string PicsCalibrationId { get; set; } = default!;
    }
}
