using System;

namespace Profile.Core.Models
{
    public class Customer : IProfile
    {
        public Customer(string name, string lastName, string phone) : this(name, lastName, phone, null) { }
        public Customer(string name, string lastName, string phone, DateTime? updatedDateUtc)
        {
            Name = name;
            LastName = lastName;
            Phone = phone;
            UpdatedDateUtc = updatedDateUtc ?? DateTime.UtcNow ;
        }

        public string Key { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }
        public Address Address { get; set; }
        public DateTime UpdatedDateUtc { get; set; }
        public string Status { get; set; }
        public ProfileType Type { get => ProfileType.Customer; }
    }

}
