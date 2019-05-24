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

        protected IndexRegistry() : this(null, null) { }
        public IndexRegistry(ILoggerFactory loggerFactory, IOptions<DynamoDBIndexRegistryOptions> options)
        {
            this.loggerFactory = loggerFactory;
            logger = loggerFactory?.CreateLogger<IndexRegistry>();
            this.options = options?.Value;
        }
        public Task Initialize()
        {
            this.storage = new DynamoDBWrapper(this.loggerFactory, this.options.Service, this.options.AccessKey, this.options.SecretKey,
                 this.options.ReadCapacityUnits, this.options.WriteCapacityUnits);

            this.logger?.LogInformation("Initializing AWS DynamoDB Indexing Table");

            return this.storage.InitializeTable(this.options.TableName);
        }

        public async Task Upsert(Type indexType, Guid objectId, string property)
        {
            if (indexType == null) return;

            await this.storage.PutEntryAsync(this.options.TableName, indexType.FullName, objectId.ToString(), property);

        }

        public Task Remove(Type indexType, Guid objectId, string property)
        {
            if (indexType == null) return Task.CompletedTask;

            return this.storage.DeleteEntryAsync(this.options.TableName, indexType.FullName, objectId.ToString(), property);
        }

        public async Task<IEnumerable<Guid>> SearchBy(Type indexType, string property)
        {
            if (indexType == null) return Array.Empty<Guid>();

            return await this.storage.QueryAsync(this.options.TableName, indexType.FullName, property);
        }
    }
}
