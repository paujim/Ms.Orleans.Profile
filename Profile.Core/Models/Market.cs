﻿using System;

namespace Profile.Core.Models
{
    public class Market : IProfile
    {
        public string ExternalId { get; set; }
        public string DisplayName { get; set; }
        public string Number { get; set; }
        public string CategoryCode { get; set; }

        public Guid Key { get; private set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public Address Address { get; set; }
        public DateTime CreationDateUtc { get; private set; }
        public DateTime UpdatedDateUtc { get; set; }
        public string Status { get; set; }
        public ProfileType Type { get => ProfileType.Market; }
    }

}
