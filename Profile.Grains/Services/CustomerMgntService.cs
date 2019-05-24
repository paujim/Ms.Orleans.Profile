using Orleans;
using Orleans.Concurrency;
using Profile.Core.Data;
using Profile.Core.Models;
using Profile.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Profile.Grains
{
    [StatelessWorker]
    public class CustomerMgntService : Grain, ICustomerMgntService
    {
        private IIndexRegistry indexRegistry;

        public CustomerMgntService(IIndexRegistry indexRegistry)
        {
            this.indexRegistry = indexRegistry;
        }

        public async Task<Guid> Create(Customer obj)
        {
            var id = Guid.NewGuid();
            var grain = GrainFactory.GetGrain<ICustomerGrain>(id);
            await grain.SetItem(obj);
            return id;
        }

        public async Task Delete(Guid id)
        {
            var grain = GrainFactory.GetGrain<ICustomerGrain>(id);
            await grain.Clear();
            return;
        }

        public async Task<Customer> GetById(Guid id)
        {
            var grain = GrainFactory.GetGrain<ICustomerGrain>(id);
            return await grain.GetItem();
        }

        public Task<IEnumerable<Guid>> SearchByProperty(string property)
        {
            return indexRegistry.SearchBy(typeof(Customer), property);
        }
    }
}
