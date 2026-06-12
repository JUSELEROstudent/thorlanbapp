namespace GotsThorlabs.Models
{
    public class TourDTO
    {
        public int IdTour { get; set; }
        public DateTime Date { get; set; }
        public string NameFolder { get; set; } = default!;
        public int NumberX { get; set; }
        public int NumberY { get; set; }
        public int NumberZ { get; set; }
        public int Camera { get; set; }
        public string? EndStatus { get; set; }
        public Guid? PicsCalibrationId { get; set; }
    }
}