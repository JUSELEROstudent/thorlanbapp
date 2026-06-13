using System;
using System.Collections.Generic;

namespace GotsThorlabs.Database.EntityRepo.Entities
{
    public partial class Tour
    {
        public Tour()
        {
            Images = new HashSet<Image>();
        }

        public long IdTour { get; set; }
        public string Date { get; set; } = null!;
        public string NameFolder { get; set; } = null!;
        public long NumberX { get; set; }
        public long NumberY { get; set; }
        public long NumberZ { get; set; }
        public long Camera { get; set; }
        public string? EndStatus { get; set; }
        public string PicsCalibrationId { get; set; } = null!;

        public virtual PicsCalibration PicsCalibration { get; set; } = null!;
        public virtual ICollection<Image> Images { get; set; }
    }
}
