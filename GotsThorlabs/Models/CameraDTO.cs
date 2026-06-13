namespace GotsThorlabs.Models
{
    public class CameraDTO
    {
        public string CameraId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string LocalIdentifier { get; set; } = default!;
        public string Features { get; set; } = default!;
        public string DriverType { get; set; } = "generic";
    }
}
