namespace GotsThorlabs.Database.EntityRepo.Entities;

public class Camera
{
    public Guid CameraId { get; set; }
    public string Name { get; set; } = default!;
    public string LocalIdentifier { get; set; } = default!;
    public string Features { get; set; } = default!;

    public ICollection<GroupCalibration> GroupCalibrations { get; set; } = new List<GroupCalibration>();
}
