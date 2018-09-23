using System;

namespace AkkaLibrary.Common.Interfaces
{
    public interface ISyncData
    {
        long TimeStamp { get; }
        uint TachometerCount { get; }
        bool MasterSyncState { get; }
        long MasterSyncIncrement { get; }
        long SampleIndex { get; }
    }
}