using Orleans;
using Orleans.Concurrency;
using Profile.Core.Data;
using Profile.Core.Models;
using Profile.Interface;
using System;
using System.Threading.Tasks;

namespace Profile.Grains
{
    [StatelessWorker]
    public class CustomerMgntService : Grain, ICustomerMgntService
    {
        private IndexRegistry indexRegistry;

        public CustomerMgntService(IndexRegistry indexRegistry)
        {
            this.indexRegistry = indexRegistry;
        }

        public async Task<Guid> CreateCustomer(Customer obj)
        {
            var id = Guid.NewGuid();
            var grain = GrainFactory.GetGrain<ICustomerGrain>(id);
            await grain.SetItem(obj);

            await indexRegistry.Upsert(nameof(ICustomerGrain), obj.Phone, id);
            await indexRegistry.Upsert(nameof(ICustomerGrain), obj.Key, id);

            return id;
        }
        public async Task<Customer> GetCustomerById(Guid id)
        {
            var grain = GrainFactory.GetGrain<ICustomerGrain>(id);
            return await grain.GetItem();
        }

        public async Task<Customer> GetCustomerByProperty(string property)
        {
            var id = await indexRegistry.ReadObject(nameof(ICustomerGrain), property);
            var grain = GrainFactory.GetGrain<ICustomerGrain>(id);
            return await grain.GetItem();
        }
    }
}
