using Orleans;
using System.Threading.Tasks;

namespace Profile.Interface
{
    public interface IStateHolderGrain<T> : IGrainWithGuidKey
    {
        Task<T> GetItem();
        Task<T> SetItem(T obj);
        Task Clear();
    }

}
