using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;
using Profile.Core.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Profile.Silo
{
    public static class GrainExtensions
    {
        public static ISiloBuilder UseGrainRegistry(this ISiloBuilder builder, Action<DynamoDBIndexRegistryOptions> configureOptions)
        {
            return builder.ConfigureServices( services =>
            {
                if (configureOptions != null)
                {
                    services.Configure(configureOptions);
                }
                services.AddSingleton<IIndexRegistry, IndexRegistry>();
            })
            .AddStartupTask<InitializeRegistry>();
        }
        public class InitializeRegistry : IStartupTask
        {
            private readonly IServiceProvider services;

            public InitializeRegistry(IServiceProvider services)
            {
                this.services = services;
            }

            public async Task Execute(CancellationToken cancellationToken)
            {
                var registry = this.services.GetRequiredService<IIndexRegistry>();
                await registry.Initialize();
            }
        }
    }
}
