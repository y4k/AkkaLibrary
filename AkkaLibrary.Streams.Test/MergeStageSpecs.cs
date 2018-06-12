using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.TestKit.Xunit2;
using AkkaLibrary.Common.Interfaces;
using AkkaLibrary.Streams.GraphStages;
using FluentAssertions;
using Xunit;

namespace AkkaLibrary.Streams.Test
{
    public class MergeStageSpecs : TestKit
    {
        [Fact]
        public void PerfectTwoStreamMerge()
        {
            using (var mat = Sys.Materializer())
            {
                var probe1 = CreateTestProbe();

                var graph = RunnableGraph.FromGraph(GraphDsl.Create(builder =>
                {
                    var source1 = Source.From(SyncDataGenerator.PrimarySyncableData());
                    var source2 = Source.From(SyncDataGenerator.SecondarySyncableData());

                    var merger = builder.Add(new MergeN<TestSyncData>(2));

                    var sink1 = Sink.ActorRef<IImmutableList<TestSyncData>>(probe1, "completed");

                    builder.From(source1).To(merger.In(0));
                    builder.From(source2).To(merger.In(1)); 

                    builder.To(sink1).From(merger.Out);

                    return ClosedShape.Instance;
                }));

                graph.Run(mat);

                var msgs = probe1.ReceiveN(98, TimeSpan.FromSeconds(3));
                msgs.Should().AllBeAssignableTo<IImmutableList<TestSyncData>>();
                var completeMsg = probe1.ReceiveOne(TimeSpan.FromSeconds(3));
                completeMsg.Should().BeAssignableTo(typeof(string));
                (completeMsg as string).Should().Be("completed");
            }
        }
    }

    public static class SyncDataGenerator
    {
        public static IEnumerable<ISyncData> PrimarySyncableData()
        {
            int sampleIndex = 0;
            return Enumerable.Range(0, 100).Select(x => 
            new TestSyncData
            {
                TimeStamp = x * 100,
                SampleIndex = sampleIndex++
            });
        }

        public static IEnumerable<ISyncData> SecondarySyncableData()
        {
            int sampleIndex = 1000;
            return Enumerable.Range(0, 100).Select(x => 
            new TestSyncData
            {
                TimeStamp = x * 100,
                SampleIndex = sampleIndex++
            });
        }
    }

    internal class TestSyncData : ISyncData
    {
        public long TimeStamp { get; set; }

        public uint TachometerCount { get; set; }

        public bool MasterSyncState { get; set; }

        public long MasterSyncIncrement { get; set; }

        public long SampleIndex { get; set; }
    }
}