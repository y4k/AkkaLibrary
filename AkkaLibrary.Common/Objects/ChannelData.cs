using System.Collections.Generic;
using System.Linq;
using AkkaLibrary.Common.BaseClasses;

namespace AkkaLibrary.Common.Objects
{
    public class ChannelData<TData> : SyncableBase
    {
        public IReadOnlyList<DataChannel<TData>> Analogs { get; }
        public IReadOnlyList<DataChannel<bool>> Digitals { get; }

        public ChannelData(long timestamp, uint tacho, long msi, bool mss, long sampleIndex) : base(timestamp, tacho, msi, mss, sampleIndex)
        {
            Analogs = new List<DataChannel<TData>>();
            Digitals = new List<DataChannel<bool>>();
        }

        public ChannelData(IEnumerable<DataChannel<TData>> analogs, IEnumerable<DataChannel<bool>> digitals, long timestamp, uint tacho, long msi, bool mss, long sampleIndex) : this(timestamp, tacho, msi, mss, sampleIndex)
        {
            Analogs = analogs.ToList();
            Digitals = digitals.ToList();
        }
    }
}