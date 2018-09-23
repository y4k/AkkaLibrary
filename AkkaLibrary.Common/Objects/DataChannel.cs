namespace AkkaLibrary
{
    public class DataChannel<TData>
    {
        public string Name { get; set; }
        public TData Value { get; set; }
        public Unit Units { get; set; }

        public DataChannel() { }

        public DataChannel(string name, TData value) : this(name, value, Unit.None) { }

        public DataChannel(string name, TData value, Unit units)
        {
            Name = name;
            Value = value;
            Units = units;
        }
    }
}