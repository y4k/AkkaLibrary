using System;
using System.Collections.Generic;
using System.Linq;
using AkkaLibrary.Common.BaseClasses;
using AkkaLibrary.Common.Utilities;

namespace AkkaLibrary
{
    public class FpgaSample : SyncableBase
    {
        public IReadOnlyList<(string name,UInt32 value)> UInt32s { get; }
        public IReadOnlyList<(string name,Int16 value)> Int16s { get; }
        public IReadOnlyList<(string name,Int24 value)> Int24s { get; }
        public IReadOnlyList<(string name,Int32 value)> Int32s { get; }
        public IReadOnlyList<(string name,float value)> Floats { get; }
        public IReadOnlyList<(string name,Double value)> Doubles { get; }
        public IReadOnlyList<(string name,bool value)> Bools { get; }

        public FpgaSample(
            long timestamp,
            uint tacho,
            long msi,
            bool mss,
            long sampleIndex,
            List<(string,UInt32)> uint32s,
            List<(string,Int16)> int16s,
            List<(string,Int24)> int24s,
            List<(string,Int32)> int32s,
            List<(string,float)> floats,
            List<(string,Double)> doubles,
            List<(string,bool)> bools
            ) : base(timestamp, tacho, msi, mss, sampleIndex)
        {
            UInt32s = uint32s;
            Int16s = int16s;
            Int24s = int24s;
            Int32s = int32s;
            Floats = floats;
            Doubles = doubles;
            Bools = bools;
        }

        public IEnumerable<(string name, float value)> GetAnalogsAsFloats()
        {
            var data = new List<(string name, float value)>();
            data.AddRange(UInt32s.Select(x => (name: x.name, value:Convert.ToSingle(x.value))));
            data.AddRange(Int16s.Select(x => (name: x.name, value:Convert.ToSingle(x.value))));
            data.AddRange(Int24s.Select(x => (name: x.name, value:(float)x.value)));
            data.AddRange(Int32s.Select(x => (name: x.name, value:Convert.ToSingle(x.value))));
            data.AddRange(Floats.Select(x => (name: x.name, value:x.value)));
            data.AddRange(Doubles.Select(x => (name: x.name, value:Convert.ToSingle(x.value))));

            return data;
        }

        public IEnumerable<(string name, double value)> GetAnalogsAsDoubles()
        {
            var data = new List<(string name, double value)>();
            data.Concat(UInt32s.Select(x => (name: x.name, value:Convert.ToDouble(x.value))));
            data.Concat(Int16s.Select(x => (name: x.name, value:Convert.ToDouble(x.value))));
            data.Concat(Int24s.Select(x => (name: x.name, value:(double)x.value)));
            data.Concat(Int32s.Select(x => (name: x.name, value:Convert.ToDouble(x.value))));
            data.Concat(Floats.Select(x => (name: x.name, value:Convert.ToDouble(x.value))));
            data.Concat(Doubles.Select(x => (name: x.name, value:x.value)));

            return data;
        }
    }
}
