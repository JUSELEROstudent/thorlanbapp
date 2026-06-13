using System;
using System.Collections.Generic;

namespace GotsThorlabs.Database.EntityRepo.Entities
{
    public partial class PicsCalibration
    {
        public PicsCalibration()
        {
            Tours = new HashSet<Tour>();
        }

        public string PicsCalibrationId { get; set; } = null!;
        public string GroupCailbrationId { get; set; } = null!;
        public string Pic1 { get; set; } = null!;
        public string Pic2 { get; set; } = null!;
        public string AxeDirectionCalibration { get; set; } = null!;
        public long Acepted { get; set; }
        public string Dx { get; set; } = null!;
        public string Dy { get; set; } = null!;
        public string Confidence { get; set; } = null!;
        public string MeasureUnit { get; set; } = null!;
        public string MovementValue { get; set; } = null!;
        public long? NumberOfSteps { get; set; }
        public string? AxisMovementName { get; set; }

        public virtual GroupCalibration GroupCailbration { get; set; } = null!;
        public virtual ICollection<Tour> Tours { get; set; }
    }
}
