using Orleans.TestingHost;
using Profile.Core;
using Profile.Core.Models;
using Profile.Interface;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Profile.Test
{
    [Collection(ClusterCollection.Name)]
    public class BussinessGrainTests
    {
        private readonly TestCluster cluster;

        public BussinessGrainTests(ClusterFixture fixture)
        {
            cluster = fixture.Cluster;
        }

        [Fact]
        public async Task TestingSetterAndGetter()
        {
            var id = Guid.NewGuid();
            var setBussiness = await cluster.GrainFactory
                .GetGrain<IBussinessGrain>(id)
                .SetItem(new Bussiness("B006843400", RandomUtils.GenerateString(10), "9864322148"));

            var getBussiness = await cluster.GrainFactory
                .GetGrain<IBussinessGrain>(id)
                .GetItem();

            Assert.Equal("B006843400", setBussiness.Number);
            Assert.Equal("B006843400", getBussiness.Number);
            Assert.Equal("9864322148", setBussiness.Phone);
            Assert.Equal("9864322148", getBussiness.Phone);
            Assert.Equal(setBussiness.Key, getBussiness.Key);

        }
    }

}
