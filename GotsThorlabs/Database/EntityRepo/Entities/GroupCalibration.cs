using System;
using System.Collections.Generic;

namespace GotsThorlabs.Database.EntityRepo.Entities
{
    public partial class GroupCalibration
    {
        public GroupCalibration()
        {
            PicsCalibrations = new HashSet<PicsCalibration>();
        }

        public string GroupCailbrationId { get; set; } = null!;
        public string CameraId { get; set; } = null!;
        public string MicroscopeId { get; set; } = null!;
        public string IncreaseId { get; set; } = null!;
        public string Date { get; set; } = null!;
        public string AditionalInfo { get; set; } = null!;

        public virtual Camera Camera { get; set; } = null!;
        public virtual Increase Increase { get; set; } = null!;
        public virtual Microscope Microscope { get; set; } = null!;
        public virtual ICollection<PicsCalibration> PicsCalibrations { get; set; }
    }
}
