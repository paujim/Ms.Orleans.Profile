using Orleans.Providers;
using Profile.Core;
using Profile.Core.Data;
using Profile.Core.Models;
using Profile.Interface;

namespace Profile.Grains
{
    [StorageProvider(ProviderName = OrleansConstants.GrainPersistenceStorage)]
    public class CustomerGrain : IndexableGrain<Customer>, ICustomerGrain
    {
        public CustomerGrain(IIndexRegistry indexRegistry) : base(indexRegistry) { }
    }
}