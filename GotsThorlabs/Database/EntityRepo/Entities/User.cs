using System;
using System.Collections.Generic;

namespace GotsThorlabs.Database.EntityRepo.Entities
{
    public partial class User
    {
        public long IdUser { get; set; }
        public string Nickname { get; set; } = null!;
        public string EMail { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
