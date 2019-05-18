using Orleans;
using Orleans.Concurrency;
using Profile.Core.Data;
using Profile.Core.Models;
using Profile.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Profile.Grains.Services
{
    [StatelessWorker]
    public class BussinessMgntService : Grain, IBussinessMgntService
    {
        private IIndexRegistry indexRegistry;

        public BussinessMgntService(IIndexRegistry indexRegistry)
        {
            this.indexRegistry = indexRegistry;
        }

        public async Task<Guid> Create(Bussiness obj)
        {
            var id = Guid.NewGuid();
            var grain = GrainFactory.GetGrain<IBussinessGrain>(id);
            await grain.SetItem(obj);
            return id;
        }

        public async Task Delete(Guid id)
        {
            var grain = GrainFactory.GetGrain<IBussinessGrain>(id);
            await grain.Clear();
            return;
        }

        public async Task<Bussiness> GetById(Guid id)
        {
            var grain = GrainFactory.GetGrain<IBussinessGrain>(id);
            return await grain.GetItem();
        }

        public Task<IEnumerable<Guid>> SearchByProperty(string property)
        {
            return indexRegistry.SearchBy(typeof(Bussiness), property);
        }
    }

}
