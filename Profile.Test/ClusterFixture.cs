using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans.Hosting;
using Orleans.TestingHost;
using Profile.Core;
using Profile.Core.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Profile.Test
{
    public class TestSiloConfigurations : ISiloBuilderConfigurator
    {
        public void Configure(ISiloHostBuilder hostBuilder)
        {
            hostBuilder
                .AddMemoryGrainStorage(OrleansConstants.GrainPersistenceStorage)
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IIndexRegistry>(GetMockIndexRegistry().Object);
                });
        }

        Mock<IIndexRegistry> GetMockIndexRegistry()
        {
            var mockIndexRegistry = new Mock<IIndexRegistry>();
            mockIndexRegistry
                .Setup( reg => reg.Initialize())
                .Returns(Task.CompletedTask);
            mockIndexRegistry
                .Setup(reg => reg.Upsert(It.IsAny<Type>(), It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mockIndexRegistry
                .Setup(reg => reg.Remove(It.IsAny<Type>(), It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mockIndexRegistry
                .Setup(reg => reg.SearchBy(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns((Type t, string s) =>
                {
                    IEnumerable<Guid> response = new Guid[] { new Guid(s) };
                    return Task.FromResult(response);
                });

            return mockIndexRegistry;
        }
    }

    public class ClusterFixture : IDisposable
    {
        public ClusterFixture()
        {
            var builder = new TestClusterBuilder(2);
            builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
            this.Cluster = builder.Build();
            this.Cluster.Deploy();
        }

        public void Dispose()
        {
            this.Cluster.StopAllSilos();
        }

        public TestCluster Cluster { get; private set; }
    }

    [CollectionDefinition(ClusterCollection.Name)]
    public class ClusterCollection : ICollectionFixture<ClusterFixture>
    {
        public const string Name = "ClusterCollection";
    }

}
