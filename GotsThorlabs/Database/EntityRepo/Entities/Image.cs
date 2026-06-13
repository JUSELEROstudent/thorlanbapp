using System;
using System.Collections.Generic;

namespace GotsThorlabs.Database.EntityRepo.Entities
{
    public partial class Image
    {
        public long IdImage { get; set; }
        public string Name { get; set; } = null!;
        public double? GausianVal { get; set; }
        public string Path { get; set; } = null!;
        public long X { get; set; }
        public long Y { get; set; }
        public long Z { get; set; }
        public long IdTour { get; set; }

        public virtual Tour IdTourNavigation { get; set; } = null!;
    }
}
