namespace GotsThorlabs.Database.EntityRepo.Entities;

public class PicsCalibration
{
    public Guid PicsCalibrationId { get; set; }
    public Guid GroupCailbrationId { get; set; }

    public string Pic1 { get; set; } = default!; // up to 1200 chars
    public string Pic2 { get; set; } = default!; // up to 1200 chars
    public string AxeDirectionCalibration { get; set; } = default!; // length 2
    public bool Acepted { get; set; }
    public decimal dx { get; set; }
    public decimal dy { get; set; }
    public decimal Confidence { get; set; }
    public string MeasureUnit { get; set; } = default!;
    public decimal MovementValue { get; set; }

    public GroupCalibration? GroupCalibration { get; set; }
}
