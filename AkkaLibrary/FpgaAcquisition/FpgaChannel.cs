namespace AkkaLibrary
{
    public class FpgaChannel
    {
        public string ChannelName { get; set; }
        public ChannelType DataType { get; set; }

        public FpgaChannel(string name, ChannelType dataType)
        {
            ChannelName = name;
            DataType = dataType;
        }

        public override bool Equals(object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;

            return Equals(obj as FpgaChannel);
        }

        public override int GetHashCode()
        {
            return (33 * (ChannelName ?? "").GetHashCode() ) + DataType.GetHashCode();
        }

        public bool Equals(FpgaChannel channel)
        {
            return channel.ChannelName.Equals(ChannelName) && channel.DataType.Equals(DataType);
        }
    }
}
