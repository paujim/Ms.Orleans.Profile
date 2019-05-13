using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;

namespace Profile.Core.Data
{
    internal class DynamoDBWrapper
    {
        private string accessKey;
        protected string secretKey;
        private string service;
        private int readCapacityUnits;
        private int writeCapacityUnits;
        private AmazonDynamoDBClient ddbClient;
        private readonly ILogger logger;

        /// <summary>
        /// Wrapper around AWS DynamoDB SDK. Cloned from main repo
        /// </summary>
        public DynamoDBWrapper(ILoggerFactory loggerFactory, string service, string accessKey, string secretKey, int readCapacityUnits, int writeCapacityUnits)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            this.accessKey = accessKey;
            this.secretKey = secretKey;
            this.service = service;
            this.readCapacityUnits = readCapacityUnits;
            this.writeCapacityUnits = writeCapacityUnits;
            this.logger = loggerFactory?.CreateLogger<DynamoDBWrapper>();
            CreateClient();
        }
        private void CreateClient()
        {

            if (service.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                service.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Local DynamoDB instance (for testing)
                var credentials = new BasicAWSCredentials("dummy", "dummyKey");
                ddbClient = new AmazonDynamoDBClient(credentials, new AmazonDynamoDBConfig { ServiceURL = service });
            }
            else if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                // AWS DynamoDB instance (auth via explicit credentials)
                var credentials = new BasicAWSCredentials(accessKey, secretKey);
                ddbClient = new AmazonDynamoDBClient(credentials, new AmazonDynamoDBConfig { ServiceURL = service, RegionEndpoint = GetRegionEndpoint(service) });
            }
            else
            {
                // AWS DynamoDB instance (implicit auth - EC2 IAM Roles etc)
                ddbClient = new AmazonDynamoDBClient(new AmazonDynamoDBConfig { ServiceURL = service, RegionEndpoint = GetRegionEndpoint(service) });
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
        /// <param name="tableName">The name of the table</param>
        /// <param name="keys">The keys definitions</param>
        /// <param name="attributes">The attributes used on the key definition</param>
        /// <param name="secondaryIndexes">(optional) The secondary index definitions</param>
        /// <returns></returns>
        public async Task InitializeTable(string tableName, List<KeySchemaElement> keys, List<AttributeDefinition> attributes, List<GlobalSecondaryIndex> secondaryIndexes = null)
        {
            try
            {
                if (await GetTableDescription(tableName) == null)
                    await CreateTable(tableName, keys, attributes, secondaryIndexes);
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

        public Task DeleTableAsync(string tableName)
        {
            try
            {
                return ddbClient.DeleteTableAsync(new DeleteTableRequest { TableName = tableName });
            }
            catch (Exception exc)
            {
                logger?.LogError($"Could not delete table {tableName}", exc);
                throw;
            }
        }

        /// <summary>
        /// Create or Replace an entry in a DynamoDB Table
        /// </summary>
        /// <param name="tableName">The name of the table to put an entry</param>
        /// <param name="fields">The fields/attributes to add or replace in the table</param>
        /// <param name="conditionExpression">Optional conditional expression</param>
        /// <param name="conditionValues">Optional field/attribute values used in the conditional expression</param>
        /// <returns></returns>
        public Task PutEntryAsync(string tableName, Dictionary<string, AttributeValue> fields, string conditionExpression = "", Dictionary<string, AttributeValue> conditionValues = null)
        {
            try
            {
                var request = new PutItemRequest(tableName, fields, ReturnValue.NONE);
                if (!string.IsNullOrWhiteSpace(conditionExpression))
                    request.ConditionExpression = conditionExpression;

                if (conditionValues != null && conditionValues.Keys.Count > 0)
                    request.ExpressionAttributeValues = conditionValues;

                return ddbClient.PutItemAsync(request);
            }
            catch (Exception exc)
            {
                logger?.LogError($"Unable to create item to table '{tableName}'", exc);
                throw;
            }
        }

        /// <summary>
        /// Create or update an entry in a DynamoDB Table
        /// </summary>
        /// <param name="tableName">The name of the table to upsert an entry</param>
        /// <param name="keys">The table entry keys for the entry</param>
        /// <param name="fields">The fields/attributes to add or updated in the table</param>
        /// <param name="conditionExpression">Optional conditional expression</param>
        /// <param name="conditionValues">Optional field/attribute values used in the conditional expression</param>
        /// <param name="extraExpression">Additional expression that will be added in the end of the upsert expression</param>
        /// <param name="extraExpressionValues">Additional field/attribute that will be used in the extraExpression</param>
        /// <remarks>The fields dictionary item values will be updated with the values returned from DynamoDB</remarks>
        /// <returns></returns>
        public async Task UpsertEntryAsync(string tableName, Dictionary<string, AttributeValue> keys, Dictionary<string, AttributeValue> fields,
            string conditionExpression = "", Dictionary<string, AttributeValue> conditionValues = null, string extraExpression = "",
            Dictionary<string, AttributeValue> extraExpressionValues = null)
        {
            try
            {
                var request = new UpdateItemRequest
                {
                    TableName = tableName,
                    Key = keys,
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>(),
                    ReturnValues = ReturnValue.UPDATED_NEW
                };

                var updateExpression = new StringBuilder();
                foreach (var field in fields.Keys)
                {
                    var valueKey = ":" + field;
                    request.ExpressionAttributeValues.Add(valueKey, fields[field]);
                    updateExpression.Append($" {field} = {valueKey},");
                }
                updateExpression.Insert(0, "SET");

                if (string.IsNullOrWhiteSpace(extraExpression))
                {
                    updateExpression.Remove(updateExpression.Length - 1, 1);
                }
                else
                {
                    updateExpression.Append($" {extraExpression}");
                    if (extraExpressionValues != null && extraExpressionValues.Count > 0)
                    {
                        foreach (var key in extraExpressionValues.Keys)
                        {
                            request.ExpressionAttributeValues.Add(key, extraExpressionValues[key]);
                        }
                    }
                }

                request.UpdateExpression = updateExpression.ToString();

                if (!string.IsNullOrWhiteSpace(conditionExpression))
                    request.ConditionExpression = conditionExpression;

                if (conditionValues != null && conditionValues.Keys.Count > 0)
                {
                    foreach (var item in conditionValues)
                    {
                        request.ExpressionAttributeValues.Add(item.Key, item.Value);
                    }
                }

                var result = await ddbClient.UpdateItemAsync(request);

                foreach (var key in result.Attributes.Keys)
                {
                    if (fields.ContainsKey(key))
                    {
                        fields[key] = result.Attributes[key];
                    }
                    else
                    {
                        fields.Add(key, result.Attributes[key]);
                    }
                }
            }
            catch (Exception exc)
            {
                logger?.LogWarning($"Error upserting to the table {tableName}", exc);
                throw;
            }
        }

        /// <summary>
        /// Delete an entry from a DynamoDB table
        /// </summary>
        /// <param name="tableName">The name of the table to delete an entry</param>
        /// <param name="keys">The table entry keys for the entry to be deleted</param>
        /// <param name="conditionExpression">Optional conditional expression</param>
        /// <param name="conditionValues">Optional field/attribute values used in the conditional expression</param>
        /// <returns></returns>
        public Task DeleteEntryAsync(string tableName, Dictionary<string, AttributeValue> keys, string conditionExpression = "", Dictionary<string, AttributeValue> conditionValues = null)
        {
            try
            {
                var request = new DeleteItemRequest
                {
                    TableName = tableName,
                    Key = keys
                };

                if (!string.IsNullOrWhiteSpace(conditionExpression))
                    request.ConditionExpression = conditionExpression;

                if (conditionValues != null && conditionValues.Keys.Count > 0)
                    request.ExpressionAttributeValues = conditionValues;

                return ddbClient.DeleteItemAsync(request);
            }
            catch (Exception exc)
            {
                logger?.LogWarning($"Error deleting entry from the table {tableName}.", exc);
                throw;
            }
        }

        /// <summary>
        /// Delete multiple entries from a DynamoDB table (Batch delete)
        /// </summary>
        /// <param name="tableName">The name of the table to delete entries</param>
        /// <param name="toDelete">List of key values for each entry that must be deleted in the batch</param>
        /// <returns></returns>
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

        /// <summary>
        /// Read an entry from a DynamoDB table
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="tableName">The name of the table to search for the entry</param>
        /// <param name="keys">The table entry keys to search for</param>
        /// <param name="resolver">Function that will be called to translate the returned fields into a concrete type. This Function is only called if the result is != null</param>
        /// <returns>The object translated by the resolver function</returns>
        public async Task<TResult> ReadSingleEntryAsync<TResult>(string tableName, Dictionary<string, AttributeValue> keys, Func<Dictionary<string, AttributeValue>, TResult> resolver) where TResult : class
        {
            try
            {
                var request = new GetItemRequest
                {
                    TableName = tableName,
                    Key = keys,
                    ConsistentRead = true
                };

                var response = await ddbClient.GetItemAsync(request);

                if (response.IsItemSet)
                {
                    return resolver(response.Item);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                logger?.LogDebug($"Unable to find table entry for Keys = {keys.ToString()}");
                throw;
            }
        }

        /// <summary>
        /// Query for multiple entries in a DynamoDB table by filtering its keys
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="tableName">The name of the table to search for the entries</param>
        /// <param name="keys">The table entry keys to search for</param>
        /// <param name="keyConditionExpression">the expression that will filter the keys</param>
        /// <param name="resolver">Function that will be called to translate the returned fields into a concrete type. This Function is only called if the result is != null and will be called for each entry that match the query and added to the results list</param>
        /// <param name="indexName">In case a secondary index is used in the keyConditionExpression</param>
        /// <param name="scanIndexForward">In case an index is used, show if the seek order is ascending (true) or descending (false)</param>
        /// <param name="lastEvaluatedKey">The primary key of the first item that this operation will evaluate. Use the value that was returned for LastEvaluatedKey in the previous operation</param>
        /// <returns>The collection containing a list of objects translated by the resolver function and the LastEvaluatedKey for paged results</returns>
        public async Task<(List<TResult> results, Dictionary<string, AttributeValue> lastEvaluatedKey)> QueryAsync<TResult>(string tableName, Dictionary<string, AttributeValue> keys, string keyConditionExpression, Func<Dictionary<string, AttributeValue>, TResult> resolver, string indexName = "", bool scanIndexForward = true, Dictionary<string, AttributeValue> lastEvaluatedKey = null) where TResult : class
        {
            try
            {
                var request = new QueryRequest
                {
                    TableName = tableName,
                    ExpressionAttributeValues = keys,
                    ConsistentRead = true,
                    KeyConditionExpression = keyConditionExpression,
                    Select = Select.ALL_ATTRIBUTES,
                    ExclusiveStartKey = lastEvaluatedKey
                };

                if (!string.IsNullOrWhiteSpace(indexName))
                {
                    request.ScanIndexForward = scanIndexForward;
                    request.IndexName = indexName;
                }

                var response = await ddbClient.QueryAsync(request);

                var resultList = new List<TResult>();
                foreach (var item in response.Items)
                {
                    resultList.Add(resolver(item));
                }
                return (resultList, response.LastEvaluatedKey);
            }
            catch (Exception)
            {
                logger?.LogDebug($"Unable to find table entry for Keys = {keys.ToString()}");
                throw;
            }
        }

        /// <summary>
        /// Scan a DynamoDB table by querying the entry fields.
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="tableName">The name of the table to search for the entries</param>
        /// <param name="attributes">The attributes used on the expression</param>
        /// <param name="expression">The filter expression</param>
        /// <param name="resolver">Function that will be called to translate the returned fields into a concrete type. This Function is only called if the result is != null and will be called for each entry that match the query and added to the results list</param>
        /// <returns>The collection containing a list of objects translated by the resolver function</returns>
        public async Task<List<TResult>> ScanAsync<TResult>(string tableName, Dictionary<string, AttributeValue> attributes, string expression, Func<Dictionary<string, AttributeValue>, TResult> resolver) where TResult : class
        {
            try
            {
                var request = new ScanRequest
                {
                    TableName = tableName,
                    ConsistentRead = true,
                    FilterExpression = expression,
                    ExpressionAttributeValues = attributes,
                    Select = Select.ALL_ATTRIBUTES
                };

                var response = await ddbClient.ScanAsync(request);

                var resultList = new List<TResult>();
                foreach (var item in response.Items)
                {
                    resultList.Add(resolver(item));
                }
                return resultList;
            }
            catch (Exception exc)
            {
                logger?.LogError($"Failed to read table {tableName}: {exc.Message}", exc);
                throw;
            }
        }

        /// <summary>
        /// Crete or replace multiple entries in a DynamoDB table (Batch put)
        /// </summary>
        /// <param name="tableName">The name of the table to search for the entry</param>
        /// <param name="toCreate">List of key values for each entry that must be created or replaced in the batch</param>
        /// <returns></returns>
        public Task PutEntriesAsync(string tableName, IReadOnlyCollection<Dictionary<string, AttributeValue>> toCreate)
        {
            if (toCreate == null)
                throw new ArgumentNullException("collection");

            if (toCreate.Count == 0)
                return Task.CompletedTask;

            try
            {
                var request = new BatchWriteItemRequest();
                request.RequestItems = new Dictionary<string, List<WriteRequest>>();
                var batch = new List<WriteRequest>();

                foreach (var item in toCreate)
                {
                    var writeRequest = new WriteRequest();
                    writeRequest.PutRequest = new PutRequest();
                    writeRequest.PutRequest.Item = item;
                    batch.Add(writeRequest);
                }
                request.RequestItems.Add(tableName, batch);
                return ddbClient.BatchWriteItemAsync(request);
            }
            catch (Exception exc)
            {
                logger?.LogWarning($"Error bulk inserting entries to table {tableName}.", exc);
                throw;
            }
        }
    }
}