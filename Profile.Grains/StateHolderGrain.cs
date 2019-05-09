using Orleans;
using Profile.Interface;
using System.Threading.Tasks;

namespace Profile.Grains
{
    public class StateHolder<T>
    {
        public StateHolder() : this(default) { }
        public StateHolder(T value)
        {
            Value = value;
        }
        public T Value { get; set; }
    }

    public abstract class StateHolderGrain<T> : Grain<StateHolder<T>>, IStateHolderGrain<T>
    {
        public async Task<T> GetItem()
        {
            await base.ReadStateAsync();
            return State.Value;
        }
        public async Task<T> SetItem(T item)
        {
            State.Value = item;
            await WriteStateAsync();
            return State.Value;
        }
    }

}
