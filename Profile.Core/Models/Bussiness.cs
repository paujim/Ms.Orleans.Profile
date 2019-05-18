using Profile.Core.Attributes;
using System;

namespace Profile.Core.Models
{
    public class Bussiness : IProfile
    {
        public Bussiness() : this(null, null, null) { }
        public Bussiness(string number, string key, string phone) : this(number, key, phone, null) { }
        public Bussiness(string number, string key, string phone, DateTime? updatedDateUtc)
        {
            Number = number;
            Key = key;
            Phone = phone;
            UpdatedDateUtc = updatedDateUtc ?? DateTime.UtcNow;
        }


        public string ExternalId { get; set; }
        public string DisplayName { get; set; }
        [SearchBy]
        public string Number { get; set; }
        public string CategoryCode { get; set; }
        [Unique]
        public string Key { get; set; }

        [SearchBy]
        public string Phone { get; set; }
        public string Email { get; set; }
        public Address Address { get; set; }
        public DateTime UpdatedDateUtc { get; set; }
        public string Status { get; set; }
        public ProfileType Type { get => ProfileType.Bussiness; }
    }

}
