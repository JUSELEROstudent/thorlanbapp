namespace GotsThorlabs.Models
{
    public class MicroscopeDTO
    {
        public Guid MicroscopeId { get; set; }
        public string Name { get; set; } = default!;
        public string Brand { get; set; } = default!;
        public string Site { get; set; } = default!;
        public string AditionalInfo { get; set; } = default!;
    }
}