namespace GotsThorlabs.Models
{
    public class CameraDTO
    {
        public Guid CameraId { get; set; }
        public string Name { get; set; } = default!;
        public string LocalIdentifier { get; set; } = default!;
        public string Features { get; set; } = default!;
        public string DriverType { get; set; } = "generic";
    }
}