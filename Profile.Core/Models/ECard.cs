using System;

namespace Profile.Core.Models
{
    public class ECard
    {
        public DateTime CreationDate { get; private set; }
        public Guid Key { get; set; }
        public string BankName { get; set; }
        public string HolderName { get; set; }
        public string ExpirationMonth { get; set; }
        public string ExpirationYear { get; set; }
        public string CardNumber { get; set; }
        public string Brand { get; set; }
        public string BankCode { get; set; }
        public ECardType Type { get; set; }
        public string DeviceSessionId { set; get; }
        public Affiliation Affiliation { set; get; }
    }

}
