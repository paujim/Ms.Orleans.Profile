using System;
using System.Collections.Generic;

namespace Profile.Core.Models
{
    public class Profile
    {
        public Guid Key { get; set; }
        public HashSet<ECard> ECards { get; set; }
        public HashSet<IProfile> Profiles { get; set; }
    }
}
