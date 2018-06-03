using AkkaLibrary.Common.Interfaces;

namespace DataSynchronisation
{
    public class TimedObject : ISyncData
    {
        public long TimeStamp { get; set; }

        public bool MasterSyncState { get; set; }

        public long MasterSyncIncrement { get; set; }

        public long SampleIndex { get; set; }

        public uint TachometerCount { get; set; }
    }
}