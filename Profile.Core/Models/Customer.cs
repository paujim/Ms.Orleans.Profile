using Profile.Core.Attributes;
using System;

namespace Profile.Core.Models
{
    public class Customer : IProfile
    {
        public Customer() : this(null, null, null) { }
        public Customer(string name, string key, string phone) : this(name, key, phone, null) { }
        public Customer(string name, string key, string phone, DateTime? updatedDateUtc)
        {
            Name = name;
            Key = key;
            Phone = phone;
            UpdatedDateUtc = updatedDateUtc ?? DateTime.UtcNow ;
        }
        [Unique]
        public string Key { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        [SearchBy]
        public string Phone { get; set; }
        public string Email { get; set; }
        public Address Address { get; set; }
        public DateTime UpdatedDateUtc { get; set; }
        public string Status { get; set; }
        public ProfileType Type { get => ProfileType.Customer; }
    }

}
