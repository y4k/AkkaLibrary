namespace AkkaLibrary
{
    public class ChannelAdjusterConfig
    {
        public string Name { get; }

        public float Scale { get; }

        public float Offset { get; }

        public FilterOption Option { get; }

        public int TemporalOffset { get; }

        public ChannelAdjusterConfig(string name, float scale, float offset, int temporalOffset, FilterOption option)
        {
            Name = name;
            Scale = scale;
            Offset = offset;
            Option = option;
            TemporalOffset = temporalOffset;
        }
    }

    public enum FilterOption
    {
        NotSet,
        PassThrough,
        CreateDigitals,
        Filter
    }
}