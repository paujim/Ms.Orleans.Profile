using Orleans;
using Profile.Core.Attributes;
using Profile.Core.Data;
using Profile.Interface;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Profile.Grains
{
    public class IndexableGrain<T> : Grain<StateHolder<T>>, IStateHolderGrain<T>
    {
        private readonly IIndexRegistry indexRegistry;

        public IndexableGrain(IIndexRegistry indexRegistry)
        {
            this.indexRegistry = indexRegistry;
        }

        public async Task Clear()
        {
            var toBeRemoved = State.Value?.GetAttributeValues<T, SearchByAttribute>() ?? Array.Empty<string>();
            foreach (var item in toBeRemoved)
            {
                await indexRegistry.Remove(State.Value?.GetType(), this.GetPrimaryKey(), item);
            }
            State.Value = default;
            await base.WriteStateAsync();
            return;
        }

        public async Task<T> GetItem()
        {
            await base.ReadStateAsync();
            return State.Value;
        }
        public async Task<T> SetItem(T item)
        {
            var oldItem = State.Value;
            State.Value = item;
            await WriteStateAsync();
            await UpdateIndices(oldItem, item);
            return State.Value;
        }
        private async Task UpdateIndices(T oldItem, T newItem)
        {
            var properties = newItem?.GetAttributeValues<T, SearchByAttribute>() ?? Array.Empty<string>();
            var toBeRemoved = oldItem?.GetAttributeValues<T, SearchByAttribute>().Except(properties) ?? Array.Empty<string>();
            foreach (var item in toBeRemoved)
            {
                await indexRegistry.Remove(oldItem?.GetType(), this.GetPrimaryKey(), item);
            }
            foreach (var property in properties)
            {
                await indexRegistry.Upsert(newItem?.GetType(), this.GetPrimaryKey(), property);
            }
        }
    }
}
