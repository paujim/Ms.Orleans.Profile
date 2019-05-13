namespace Profile.Core.Data
{
    public class DynamoDBIndexRegistryOptions
    {
        private const int DefaultReadCapacityUnits = 10;
        private const int DefaultWriteCapacityUnits = 5;

        /// <summary>
        /// AccessKey string for DynamoDB Storage
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Secret key for DynamoDB storage
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// DynamoDB Service name 
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// Read capacity unit for DynamoDB storage
        /// </summary>
        public int ReadCapacityUnits { get; set; } = DefaultReadCapacityUnits;

        /// <summary>
        /// Write capacity unit for DynamoDB storage
        /// </summary>
        public int WriteCapacityUnits { get; set; } = DefaultWriteCapacityUnits;

        /// <summary>
        /// DynamoDB table name.
        /// Defaults to 'OrleansIndices'.
        /// </summary>
        public string TableName { get; set; } = "OrleansIndices";
    }

}
