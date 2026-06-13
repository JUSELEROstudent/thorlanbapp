using System;
using System.Collections.Generic;

namespace GotsThorlabs.Database.EntityRepo.Entities
{
    public partial class Microscope
    {
        public Microscope()
        {
            GroupCalibrations = new HashSet<GroupCalibration>();
        }

        public string MicroscopeId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public string Site { get; set; } = null!;
        public string AditionalInfo { get; set; } = null!;

        public virtual ICollection<GroupCalibration> GroupCalibrations { get; set; }
    }
}
