namespace GotsThorlabs.Database.EntityRepo.Entities;

public class Microscope
{
    public Guid MicroscopeId { get; set; }
    public string Name { get; set; } = default!;
    public string Brand { get; set; } = default!;
    public string Site { get; set; } = default!;
    public string AditionalInfo { get; set; } = default!;

    public ICollection<GroupCalibration> GroupCalibrations { get; set; } = new List<GroupCalibration>();
}
