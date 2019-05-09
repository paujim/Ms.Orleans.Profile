using System;

namespace Profile.Core.Models
{
    public interface IProfile
    {
        string Email { get; set; }
        string Phone { get; set; }
        Address Address { get; set; }
        DateTime UpdatedDateUtc { get; set; }
        string Status { get; set; }
        ProfileType Type { get; }
    }

}
