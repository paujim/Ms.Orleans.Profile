using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Profile.Core.Data
{
    public class IndexRegistry : IIndexRegistry
    {
        private ILoggerFactory loggerFactory;
        private ILogger<IndexRegistry> logger;
        private DynamoDBIndexRegistryOptions options;
        private DynamoDBWrapper storage;

        private const string OBJ_ID = "ObjectId";
        private const string OBJ_TYPE = "ObjectType";
        private const string OBJ_PROPERTY = "ObjectProperty";

        public IndexRegistry(ILoggerFactory loggerFactory, IOptions<DynamoDBIndexRegistryOptions> options)
        {
            this.loggerFactory = loggerFactory;
            logger = loggerFactory?.CreateLogger<IndexRegistry>();
            this.options = options.Value;
        }
        public Task Initialize()
        {
            this.storage = new DynamoDBWrapper(this.loggerFactory, this.options.Service, this.options.AccessKey, this.options.SecretKey,
                 this.options.ReadCapacityUnits, this.options.WriteCapacityUnits);

            this.logger?.LogInformation("Initializing AWS DynamoDB Indexing Table");

            return this.storage.InitializeTable(this.options.TableName,
                new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = OBJ_TYPE, KeyType = KeyType.HASH },
                    new KeySchemaElement { AttributeName = OBJ_PROPERTY, KeyType = KeyType.RANGE }
                },
                new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = OBJ_TYPE, AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = OBJ_PROPERTY, AttributeType = ScalarAttributeType.S },
                    //new AttributeDefinition { AttributeName = OBJ_ID, AttributeType = ScalarAttributeType.S }
                });
        }
        public async Task Upsert(string indexType, string property, Guid objectId)
        {
            var fields = new Dictionary<string, AttributeValue>
            {
                { OBJ_TYPE, new AttributeValue ( indexType ) },
                { OBJ_PROPERTY, new AttributeValue(property) },
                { OBJ_ID, new AttributeValue(objectId.ToString()) }
            };
            await this.storage.PutEntryAsync(this.options.TableName, fields);
        }
        public async Task Remove(string indexType, string property)
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { OBJ_TYPE, new AttributeValue ( indexType ) },
                { OBJ_PROPERTY, new AttributeValue(property) }
            };
            await this.storage.DeleteEntryAsync(this.options.TableName, key).ConfigureAwait(false);
            return;
        }
        public async Task<Guid> ReadObject(string indexType, string property)
        {
            var keys = new Dictionary<string, AttributeValue>
            {
                { OBJ_TYPE, new AttributeValue(indexType) },
                { OBJ_PROPERTY, new AttributeValue (property) }
            };

            var id = await this.storage.ReadSingleEntryAsync(this.options.TableName, keys, (item) => item[OBJ_ID].S).ConfigureAwait(false);
            return new Guid(id);
        }
    }
}
