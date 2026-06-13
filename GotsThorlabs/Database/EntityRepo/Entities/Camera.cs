using System;
using System.Collections.Generic;

namespace GotsThorlabs.Database.EntityRepo.Entities
{
    public partial class Camera
    {
        public Camera()
        {
            GroupCalibrations = new HashSet<GroupCalibration>();
        }

        public string CameraId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string LocalIdentifier { get; set; } = null!;
        public string Features { get; set; } = null!;
        public string DriverType { get; set; } = null!;

        public virtual ICollection<GroupCalibration> GroupCalibrations { get; set; }
    }
}
