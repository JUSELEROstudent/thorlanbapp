using System;
using System.Collections.Generic;

namespace GotsThorlabs.Database.EntityRepo.Entities
{
    public partial class Increase
    {
        public Increase()
        {
            GroupCalibrations = new HashSet<GroupCalibration>();
        }

        public string IncreaseId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Value { get; set; } = null!;
        public string AditionalInfo { get; set; } = null!;

        public virtual ICollection<GroupCalibration> GroupCalibrations { get; set; }
    }
}
