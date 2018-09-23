using AkkaLibrary.Common.Interfaces;

namespace AkkaLibrary.Common.BaseClasses
{
    /// <summary>
    /// Abstract implementation of the <see cref="ISyncData"/> interface with
    /// a base constructor
    /// </summary>
    public abstract class SyncableBase : ISyncData
    {
        public long TimeStamp { get; }
        public uint TachometerCount { get; }
        public bool MasterSyncState { get; }
        public long MasterSyncIncrement { get; }
        public long SampleIndex { get; }

        protected SyncableBase(long timestamp, uint tacho, long msi, bool mss, long sampleIndex)
        {
            TimeStamp = timestamp;
            TachometerCount = tacho;
            MasterSyncIncrement = msi;
            MasterSyncState = mss;
            SampleIndex = sampleIndex;
        }
    }
}