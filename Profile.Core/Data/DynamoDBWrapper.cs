using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Profile.Core.Data
{
    internal class DynamoDBWrapper
    {
        private const string OBJ_UNIQUE_ID = "ObjectUniqueId";
        private const string OBJ_TYPE_PROPERTY = "ObjectTypeProperty";
        private const string INDEX_NAME = "ObjectIndex";

        private int readCapacityUnits;
        private int writeCapacityUnits;
        private readonly AmazonDynamoDBClient ddbClient;
        private readonly ILogger logger;

        /// <summary>
        /// Helper around AWS DynamoDB SDK. Copied from main repo
        /// </summary>
        public DynamoDBWrapper(ILoggerFactory loggerFactory, string service, string accessKey, string secretKey, int readCapacityUnits, int writeCapacityUnits)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            this.readCapacityUnits = readCapacityUnits;
            this.writeCapacityUnits = writeCapacityUnits;
            this.logger = loggerFactory?.CreateLogger<DynamoDBWrapper>();
            ddbClient = CreateClient(accessKey, secretKey, service);
        }
        private AmazonDynamoDBClient CreateClient(string accessKey, string secretKey, string service)
        {
            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                // AWS DynamoDB instance (auth via explicit credentials)
                var credentials = new BasicAWSCredentials(accessKey, secretKey);
                return new AmazonDynamoDBClient(credentials, new AmazonDynamoDBConfig { ServiceURL = service, RegionEndpoint = GetRegionEndpoint(service) });
            }
            else
            {
                // AWS DynamoDB instance (implicit auth - EC2 IAM Roles etc)
                return new AmazonDynamoDBClient(new AmazonDynamoDBConfig { ServiceURL = service, RegionEndpoint = GetRegionEndpoint(service) });
            }
        }
        private static RegionEndpoint GetRegionEndpoint(string zone = "")
        {
            //
            // Keep the order from RegionEndpoint so it is easier to maintain.
            // us-west-2 is the default
            //

            switch (zone)
            {
                case "us-east-1":
                    return RegionEndpoint.USEast1;
                case "ca-central-1":
                    return RegionEndpoint.CACentral1;
                case "cn-north-1":
                    return RegionEndpoint.CNNorth1;
                case "us-gov-west-1":
                    return RegionEndpoint.USGovCloudWest1;
                case "sa-east-1":
                    return RegionEndpoint.SAEast1;
                case "ap-southeast-1":
                    return RegionEndpoint.APSoutheast1;
                case "ap-south-1":
                    return RegionEndpoint.APSouth1;
                case "ap-northeast-2":
                    return RegionEndpoint.APNortheast2;
                case "ap-southeast-2":
                    return RegionEndpoint.APSoutheast2;
                case "eu-central-1":
                    return RegionEndpoint.EUCentral1;
                case "eu-west-2":
                    return RegionEndpoint.EUWest2;
                case "eu-west-1":
                    return RegionEndpoint.EUWest1;
                case "us-west-1":
                    return RegionEndpoint.USWest1;
                case "us-east-2":
                    return RegionEndpoint.USEast2;
                case "ap-northeast-1":
                    return RegionEndpoint.APNortheast1;
                default:
                    return RegionEndpoint.USWest2;
            }
        }

        /// <summary>
        /// Create a DynamoDB table if it doesn't exist
        /// </summary>
        public async Task InitializeTable(string tableName)
        {
            var keys = new List<KeySchemaElement>
            {
                new KeySchemaElement { AttributeName = OBJ_UNIQUE_ID, KeyType = KeyType.HASH },
                new KeySchemaElement { AttributeName = OBJ_TYPE_PROPERTY, KeyType = KeyType.RANGE }
            };
            var attributes = new List<AttributeDefinition>
            {
                new AttributeDefinition { AttributeName = OBJ_UNIQUE_ID, AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = OBJ_TYPE_PROPERTY, AttributeType = ScalarAttributeType.S },
            };
            var secondaryIndex = new List<GlobalSecondaryIndex>() {
                new GlobalSecondaryIndex()
                {
                    IndexName = INDEX_NAME,
                    Projection = new Projection { ProjectionType = ProjectionType.KEYS_ONLY },
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement { AttributeName = OBJ_TYPE_PROPERTY, KeyType = KeyType.HASH},
                    }
                }
            };

            try
            {
                if (await GetTableDescription(tableName) == null)
                    await CreateTable(tableName, keys, attributes, secondaryIndex);
            }
            catch (Exception exc)
            {
                logger?.LogError($"Could not initialize connection to storage table {tableName}", exc);
                throw;
            }
        }
        private async Task<TableDescription> GetTableDescription(string tableName)
        {
            try
            {
                var description = await ddbClient.DescribeTableAsync(tableName);
                if (description.Table != null)
                    return description.Table;
            }
            catch (ResourceNotFoundException)
            {
                return null;
            }
            return null;
        }
        private async Task CreateTable(string tableName, List<KeySchemaElement> keys, List<AttributeDefinition> attributes, List<GlobalSecondaryIndex> secondaryIndexes = null)
        {
            var request = new CreateTableRequest
            {
                TableName = tableName,
                AttributeDefinitions = attributes,
                KeySchema = keys,
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = readCapacityUnits,
                    WriteCapacityUnits = writeCapacityUnits
                }
            };

            if (secondaryIndexes != null && secondaryIndexes.Count > 0)
            {
                var indexThroughput = new ProvisionedThroughput { ReadCapacityUnits = readCapacityUnits, WriteCapacityUnits = writeCapacityUnits };
                secondaryIndexes.ForEach(i =>
                {
                    i.ProvisionedThroughput = indexThroughput;
                });
                request.GlobalSecondaryIndexes = secondaryIndexes;
            }

            try
            {
                var response = await ddbClient.CreateTableAsync(request);
                TableDescription description = null;
                do
                {
                    description = await GetTableDescription(tableName);

                    await Task.Delay(2000);

                } while (description.TableStatus == TableStatus.CREATING);

                if (description.TableStatus != TableStatus.ACTIVE)
                    throw new InvalidOperationException($"Failure creating table {tableName}");
            }
            catch (Exception exc)
            {
                logger?.LogError($"Could not create table {tableName}", exc);
                throw;
            }
        }

        /// <summary>
        /// Create or Replace an entry in a DynamoDB Table
        /// </summary>
        public Task PutEntryAsync(string tableName, string indexType, string objectId, string property)
        {
            var fields = new Dictionary<string, AttributeValue>
            {
                { OBJ_UNIQUE_ID, new AttributeValue(objectId) },
                { OBJ_TYPE_PROPERTY, new AttributeValue($"{indexType}_{property}")}
            };

            var request = new PutItemRequest(tableName, fields, ReturnValue.NONE);
            return ddbClient.PutItemAsync(request);
        }

        /// <summary>
        /// Delete an entry from a DynamoDB table
        /// </summary>
        public Task DeleteEntryAsync(string tableName, string indexType, string objectId, string property)
        {
            var keys = new Dictionary<string, AttributeValue>
            {
                { OBJ_UNIQUE_ID, new AttributeValue(objectId.ToString()) },
                { OBJ_TYPE_PROPERTY, new AttributeValue($"{indexType}_{property}")}
            };

            var request = new DeleteItemRequest
            {
                TableName = tableName,
                Key = keys
            };
            return ddbClient.DeleteItemAsync(request);
        }

        /// <summary>
        /// Delete multiple entries from a DynamoDB table (Batch delete)
        /// </summary>
        public Task DeleteEntriesAsync(string tableName, IReadOnlyCollection<Dictionary<string, AttributeValue>> toDelete)
        {
            if (toDelete == null)
                throw new ArgumentNullException("collection");

            if (toDelete.Count == 0)
                return Task.CompletedTask;

            try
            {
                var request = new BatchWriteItemRequest();
                request.RequestItems = new Dictionary<string, List<WriteRequest>>();
                var batch = new List<WriteRequest>();

                foreach (var keys in toDelete)
                {
                    var writeRequest = new WriteRequest();
                    writeRequest.DeleteRequest = new DeleteRequest();
                    writeRequest.DeleteRequest.Key = keys;
                    batch.Add(writeRequest);
                }
                request.RequestItems.Add(tableName, batch);
                return ddbClient.BatchWriteItemAsync(request);
            }
            catch (Exception exc)
            {
                logger?.LogWarning($"Error deleting entries from the table {tableName}.", exc);
                throw;
            }
        }

        public async Task<List<Guid>> QueryAsync(string tableName, string indexType, string property)
        {
            QueryRequest queryRequest = new QueryRequest
            {
                TableName = tableName,
                IndexName = INDEX_NAME,
                KeyConditionExpression = "#obj_prop = :v_obj_prop ",
                ExpressionAttributeNames = new Dictionary<String, String> { { "#obj_prop", OBJ_TYPE_PROPERTY } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":v_obj_prop", new AttributeValue { S = $"{indexType}_{property}" } }, },
                ScanIndexForward = true
            };
            var response = await ddbClient.QueryAsync(queryRequest);

            var resultList = new List<Guid>();
            foreach (var item in response.Items)
            {
                var id = item[OBJ_UNIQUE_ID].S;
                resultList.Add(new Guid(id));
            }
            return resultList;
        }
    }
}