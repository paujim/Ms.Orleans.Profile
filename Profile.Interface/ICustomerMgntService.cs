using Orleans;
using Profile.Core.Models;
using System;
using System.Threading.Tasks;

namespace Profile.Interface
{
    public interface ICustomerMgntService: IGrainWithIntegerKey
    {
        Task<Guid> CreateCustomer(Customer obj);
        Task<Customer> GetCustomerById(Guid id);
        //Task<Customer> GetCustomerByPhone(Guid id);
    }
}
