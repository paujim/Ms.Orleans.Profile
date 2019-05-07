using System;
using System.Collections.Generic;
using System.Text;

namespace Profile.Silo
{
    /// <summary>
    /// Contains properties utilized for configuration Orleans
    /// Clients and Cluster Nodes.
    /// </summary>
    public class SiloConfig
    {
        public int GatewayPort { get; set; }
        public int SiloPort { get; set; }
        public string AwsAccessKey { get; set; }
        public string AwsSecretKey { get; set; }
        public string AwsRegion { get; set; }
        public string AwsClusterTableName { get; set; }
        public string AwsStorageTableName { get; set; }
    }
}
