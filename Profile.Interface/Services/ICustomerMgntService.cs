using Orleans;
using Profile.Core.Models;

namespace Profile.Interface
{
    public interface ICustomerMgntService: IGrainWithIntegerKey, IMgntService<Customer> { }
}
