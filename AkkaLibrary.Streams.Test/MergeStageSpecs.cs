using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.TestKit.Xunit2;
using AkkaLibrary.Common.Interfaces;
using AkkaLibrary.Streams.GraphStages;
using FluentAssertions;
using Moq;
using Xunit;

namespace AkkaLibrary.Streams.Test
{
    public class MergeStageSpecs : TestKit
    {
        [Fact]
        public void SinglePrimarySampleMerge()
        {
            using (var mat = Sys.Materializer())
            {
                var probe = CreateTestProbe();

                var graph = RunnableGraph.FromGraph(
                    GraphDsl.Create(builder =>
                    {
                        var primary = Source.From(
                            Enumerable.Range(0,4)
                            .Select(y => Mock.Of<ISyncData>(x => x.TimeStamp == 100 * y)));

                        var secondary = Source.From(
                            Enumerable.Range(0,4)
                            .Select(y => Mock.Of<ISyncData>(x => x.TimeStamp == 100 * y + 49)));

                        var merger = builder.Add(new MergeClosestN<ISyncData>(2));

                        var sink = Sink.ActorRef<IImmutableList<ISyncData>>(probe, "completed");

                        builder.From(primary).To(merger.In(0));
                        builder.From(secondary).To(merger.In(1));
                        builder.From(merger.Out).To(sink);

                        return ClosedShape.Instance;
                    }));

                    graph.Run(mat);

                    var msgs = probe.ReceiveN(2, TimeSpan.FromSeconds(Debugger.IsAttached ? 300 : 3));
            }
        }

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

                    var merger = builder.Add(new MergeClosestN<TestSyncData>(2));

                    var sink1 = Sink.ActorRef<IImmutableList<TestSyncData>>(probe1, "completed");

                    builder.From(source1).To(merger.In(0));
                    builder.From(source2).To(merger.In(1)); 

                    builder.To(sink1).From(merger.Out);

                    return ClosedShape.Instance;
                }));

                graph.Run(mat);

                var msgs = probe1.ReceiveN(50, TimeSpan.FromSeconds(3));
                msgs.Should().AllBeAssignableTo<IImmutableList<TestSyncData>>();
                var completeMsg = probe1.ReceiveOne(TimeSpan.FromSeconds(3));
                completeMsg.Should().BeAssignableTo(typeof(string));
                (completeMsg as string).Should().Be("completed");
            }
        }
    }

    public static class SyncDataGenerator
    {
        public static IEnumerable<TestSyncData> PrimarySyncableData()
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

    public class TestSyncData : ISyncData
    {
        public long TimeStamp { get; set; }

        public uint TachometerCount { get; set; }

        public bool MasterSyncState { get; set; }

        public long MasterSyncIncrement { get; set; }

        public long SampleIndex { get; set; }
    }
}