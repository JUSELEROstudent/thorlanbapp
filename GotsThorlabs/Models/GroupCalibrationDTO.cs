namespace GotsThorlabs.Models
{
    public class GroupCalibrationDTO
    {
        public Guid GroupCailbrationId { get; set; }
        public Guid CameraId { get; set; }
        public Guid MicroscopeId { get; set; }
        public Guid IncreaseId { get; set; }
        public DateTime Date { get; set; }
        public string AditionalInfo { get; set; } = default!;
    }
}