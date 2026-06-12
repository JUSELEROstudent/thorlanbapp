namespace GotsThorlabs.Models
{
    public class IncreaseDTO
    {
        public Guid IncreaseId { get; set; }
        public string Name { get; set; } = default!;
        public decimal Value { get; set; }
        public string AditionalInfo { get; set; } = default!;
    }
}