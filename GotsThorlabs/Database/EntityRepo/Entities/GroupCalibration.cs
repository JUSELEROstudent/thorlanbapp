namespace GotsThorlabs.Database.EntityRepo.Entities;

public class GroupCalibration
{
    public Guid GroupCailbrationId { get; set; }

    public Guid CameraId { get; set; }
    public Guid MicroscopeId { get; set; }
    public Guid IncreaseId { get; set; }

    public DateTime Date { get; set; }
    public string AditionalInfo { get; set; } = default!;

    public Camera? Camera { get; set; }
    public Microscope? Microscope { get; set; }
    public Increase? Increase { get; set; }

    public ICollection<PicsCalibration> PicsCalibrations { get; set; } = new List<PicsCalibration>();
}
