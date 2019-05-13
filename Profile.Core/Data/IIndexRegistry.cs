using System;
using System.Threading.Tasks;

namespace Profile.Core.Data
{
    interface IIndexRegistry
    {
        Task Initialize();
        Task Upsert(string indexType, string property, Guid objectId);
        Task Remove(string indexType, string property);
        Task<Guid> ReadObject(string indexType, string property);
    }
}
