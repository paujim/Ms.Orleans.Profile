using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Profile.Core;
using Profile.Core.Models;
using Profile.Core.Search;
using Profile.Interface;
using System.Threading.Tasks;

namespace Profile.Grains
{
    [StorageProvider(ProviderName = OrleansConstants.GrainMemoryStorage)]
    public class CustomerGrain : StateHolderGrain<Customer>, ICustomerGrain
    {
        
    }
}
