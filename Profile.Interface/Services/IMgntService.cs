using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Profile.Interface
{
    public interface IMgntService<T> : IGrainWithIntegerKey
    {
        Task<Guid> Create(T obj);
        Task Delete(Guid id);

        Task<T> GetById(Guid id);
        Task<IEnumerable<Guid>> SearchByProperty(string property);
    }
}
