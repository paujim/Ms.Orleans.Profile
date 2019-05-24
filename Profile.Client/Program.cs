using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;
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
            Console.Title = "Test Client";
            System.Threading.Thread.Sleep(2000);

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
                    .ConfigureLogging(logging => logging.AddConsole())
                    .Build();


                await client.Connect(CreateRetryFilter());
                Console.WriteLine("Client successfully connect to silo host");

                var bussinessMgnt = client.GetGrain<IBussinessMgntService>(0L);
                var id = await bussinessMgnt.Create(new Bussiness("A006843400", RandomUtils.GenerateString(10), "9864322148")
                {
                    Address = new Address()
                    {
                        CountryCode = "AU",
                        City = "Melbourne",
                        Line1 = "Address line",
                        State = "VIC",
                        PostalCode = "3000"
                    }
                });

                var grainId = await bussinessMgnt.Create(new Bussiness("B006843400", RandomUtils.GenerateString(10), "9864322148")
                {
                    Address = new Address()
                    {
                        CountryCode = "AU",
                        City = "Melbourne",
                        Line1 = "Address line",
                        State = "VIC",
                        PostalCode = "3000"
                    }
                });

                var arrayBussiness1 = await bussinessMgnt.SearchByProperty("9864322148");

                await bussinessMgnt.Delete(id);

                var arrayBussiness2 = await bussinessMgnt.SearchByProperty("9864322148");

                Console.ReadKey();
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
