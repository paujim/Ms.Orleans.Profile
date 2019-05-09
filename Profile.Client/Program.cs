using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Profile.Core;
using Profile.Core.Models;
using Profile.Interface;
using System;
using System.Threading.Tasks;

namespace Profile.Client
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                var siloConfig = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build()
                    .GetSection("SiloConfig").Get<SiloConfig>();


                // Configure a client and connect to the service.
                var client = new ClientBuilder()
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = OrleansConstants.ClusterId;
                        options.ServiceId = OrleansConstants.ServiceId;
                    })
                    // Clustering provider
                    .UseDynamoDBClustering(options =>
                    {
                        options.AccessKey = siloConfig.AwsAccessKey;
                        options.SecretKey = siloConfig.AwsSecretKey;
                        options.Service = siloConfig.AwsRegion;
                        options.TableName = siloConfig.AwsClusterTableName;
                    })
                    .ConfigureLogging(logging => logging.AddConsole())
                    .Build();

                await client.Connect(CreateRetryFilter());
                Console.WriteLine("Client successfully connect to silo host");

                // Create new grain
                var grainKey = Guid.NewGuid();
                var customerGrain = client.GetGrain<ICustomerGrain>(grainKey);
                var customer = new Customer("name", "lastName", "phone");
                await customerGrain.SetItem(customer);
                //var response = await friend.SayHello("Good morning, my friend!");
                //Console.WriteLine("\n\n{0}\n\n", response);

                Console.ReadKey();
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
                return 1;
            }
        }
        private static Func<Exception, Task<bool>> CreateRetryFilter(int maxAttempts = 5)
        {
            var attempt = 0;
            return RetryFilter;

            async Task<bool> RetryFilter(Exception exception)
            {
                attempt++;
                Console.WriteLine($"Cluster client attempt {attempt} of {maxAttempts} failed to connect to cluster.  Exception: {exception}");
                if (attempt > maxAttempts)
                {
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(4));
                return true;
            }
        }
    }
}
