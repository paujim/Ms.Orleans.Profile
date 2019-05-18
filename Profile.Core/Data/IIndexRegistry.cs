using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Profile.Core.Data
{
    public interface IIndexRegistry
    {
        Task Initialize();
        Task Upsert(Type indexType, Guid objectId, string property);
        Task Remove(Type indexType, Guid objectId, string property);
        Task<IEnumerable<Guid>> SearchBy(Type indexType, string property);
    }
}
