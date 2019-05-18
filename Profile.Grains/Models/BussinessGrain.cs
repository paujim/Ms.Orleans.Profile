using Orleans.Providers;
using Profile.Core;
using Profile.Core.Data;
using Profile.Core.Models;
using Profile.Interface;

namespace Profile.Grains
{
    [StorageProvider(ProviderName = OrleansConstants.GrainPersistenceStorage)]
    public class BussinessGrain : IndexableGrain<Bussiness>, IBussinessGrain
    {
        public BussinessGrain(IIndexRegistry indexRegistry) : base(indexRegistry) { }
    }
}
