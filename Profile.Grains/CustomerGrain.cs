using Orleans.Providers;
using Profile.Core;
using Profile.Core.Models;
using Profile.Interface;

namespace Profile.Grains
{
    [StorageProvider(ProviderName = OrleansConstants.GrainPersistenceStorage)]
    public class CustomerGrain : StateHolderGrain<Customer>, ICustomerGrain { }
}