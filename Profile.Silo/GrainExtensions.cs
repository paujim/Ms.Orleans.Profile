using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans.Hosting;
using Profile.Core.Data;
using System;

namespace Profile.Silo
{
    public static class GrainExtensions
    {
        public static IHostBuilder UseGrainRegistry(this IHostBuilder builder, Action<DynamoDBIndexRegistryOptions> configureOptions)
        {
            return builder.ConfigureServices(
                services =>
                {
                    if (configureOptions != null)
                    {
                        services.Configure(configureOptions);
                    }
                    services.AddSingleton<IIndexRegistry, IndexRegistry>();
                });
        }

        public static IHostBuilder UseGrainRegistry(this IHostBuilder builder, Action<OptionsBuilder<DynamoDBIndexRegistryOptions>> configureOptions)
        {
            return builder.ConfigureServices(
                services =>
                {
                    configureOptions?.Invoke(services.AddOptions<DynamoDBIndexRegistryOptions>());
                    services.AddSingleton<IIndexRegistry, IndexRegistry>();
                });
        }
    }
}
