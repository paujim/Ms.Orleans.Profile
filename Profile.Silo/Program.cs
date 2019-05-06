using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Profile.Silo
{

    class Program
    {
        private static Task Main(string[] args)
        {

            Console.Title = nameof(Silo);

            return new HostBuilder()
                .UseOrleans(builder => builder
                // Clustering information
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "cluster-id";
                    options.ServiceId = "service=id";
                })
                // Clustering provider
                .UseDynamoDBClustering(options =>
                {
                    options.AccessKey = "MY_ACCESS_KEY";
                    options.SecretKey = "MY_SECRET_KEY";
                    options.Service = "us-wes-1";
                    options.TableName = "OrleansSilos";
                })
                // Endpoints
                .Configure<EndpointOptions>(options =>
                {
                    // Port to use for Silo-to-Silo
                    options.SiloPort = 11111;
                    // Port to use for the gateway
                    options.GatewayPort = 30000;
                    // IP Address to advertise in the cluster
                    options.AdvertisedIPAddress = IPAddress.Parse("172.16.0.42");
                    // The socket used for silo-to-silo will bind to this endpoint
                    options.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Any, 40000);
                    // The socket used by the gateway will bind to this endpoint
                    options.SiloListeningEndpoint = new IPEndPoint(IPAddress.Any, 50000);
                })
                .AddDynamoDBGrainStorage("DDBStore", options =>
                {
                    options.AccessKey = "MY_ACCESS_KEY";
                    options.SecretKey = "MY_SECRET_KEY";
                    options.Service = "us-wes-1";
                    options.TableName = "OrleansGrainState";
                })
                //.ConfigureApplicationParts(_ => _.AddApplicationPart(typeof(Gain).Assembly).WithReferences())
                .AddMemoryGrainStorageAsDefault()
                //.AddMemoryGrainStorage("PubSubStore")
                .UseDashboard())
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
