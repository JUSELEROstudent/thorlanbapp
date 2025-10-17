namespace GotsThorlabs.Database.EntityRepo.Entities;

public class Increase
{
    public Guid IncreaseId { get; set; }
    public string Name { get; set; } = default!;
    public decimal Value { get; set; }
    public string AditionalInfo { get; set; } = default!;

    public ICollection<GroupCalibration> GroupCalibrations { get; set; } = new List<GroupCalibration>();
}
