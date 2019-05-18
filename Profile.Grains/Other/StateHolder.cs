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
}
