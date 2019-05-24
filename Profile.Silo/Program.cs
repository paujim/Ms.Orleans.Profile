using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;
using Profile.Core;
using Profile.Grains;
using System;
using System.Threading.Tasks;

namespace Profile.Silo
{

    class Program
    {
        private static Task Main(string[] args)
        {
            Console.Title = nameof(Silo);

            var config = new ConfigurationBuilder()
             .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
             .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
             .AddEnvironmentVariables()
             .Build();

            var siloConfig = config.GetSection("SiloConfig").Get<SiloConfig>();

            return new HostBuilder()
                .UseOrleans(builder => builder
                    // Clustering information
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = OrleansConstants.ClusterId;
                        options.ServiceId = OrleansConstants.ServiceId;
                    })
                    .Configure<SerializationProviderOptions>(options =>
                    {
                        options.SerializationProviders.Add(typeof(ProtobufSerializer));
                    })
                    // Clustering provider
                    .UseDynamoDBClustering(options =>
                    {
                        options.AccessKey = siloConfig.AwsAccessKey;
                        options.SecretKey = siloConfig.AwsSecretKey;
                        options.Service = siloConfig.AwsRegion;
                        options.TableName = siloConfig.AwsClusterTableName;
                    })
                    // Endpoints
                    .ConfigureEndpoints(siloPort: siloConfig.SiloPort, gatewayPort: siloConfig.GatewayPort)
                    //.Configure<EndpointOptions>(options =>
                    //{
                    //    // Port to use for Silo-to-Silo
                    //    options.SiloPort = 11111;
                    //    // Port to use for the gateway
                    //    options.GatewayPort = 30000;
                    //    // IP Address to advertise in the cluster
                    //    options.AdvertisedIPAddress = IPAddress.Parse("172.16.0.42");
                    //    // The socket used for silo-to-silo will bind to this endpoint
                    //    options.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Any, 40000);
                    //    // The socket used by the gateway will bind to this endpoint
                    //    options.SiloListeningEndpoint = new IPEndPoint(IPAddress.Any, 50000);
                    //})
                    .AddDynamoDBGrainStorage(OrleansConstants.GrainPersistenceStorage, options =>
                    {
                        options.AccessKey = siloConfig.AwsAccessKey;
                        options.SecretKey = siloConfig.AwsSecretKey;
                        options.Service = siloConfig.AwsRegion;
                        options.TableName = siloConfig.AwsStorageTableName;
                    })
                    .ConfigureApplicationParts(_ => _.AddApplicationPart(typeof(CustomerGrain).Assembly).WithReferences())
                    .AddMemoryGrainStorageAsDefault()
                    .AddMemoryGrainStorage(OrleansConstants.GrainMemoryStorage)
                    .UseDashboard( )
                    .UseGrainRegistry(options =>
                    {
                        options.AccessKey = siloConfig.AwsAccessKey;
                        options.SecretKey = siloConfig.AwsSecretKey;
                        options.Service = siloConfig.AwsRegion;
                    }))
                .ConfigureServices(services =>
                {
                    //services.AddHostedService<StocksHostedService>();
                    //services.Configure<ConsoleLifetimeOptions>(options =>
                    //{
                    //    options.SuppressStatusMessages = true;
                    //});
                })
                .ConfigureLogging(builder => builder
                    .AddFilter("Orleans.Runtime.Management.ManagementGrain", LogLevel.Warning)
                    .AddFilter("Orleans.Runtime.SiloControl", LogLevel.Warning)
                    .AddConsole())
                .RunConsoleAsync();
        }
    }
}
