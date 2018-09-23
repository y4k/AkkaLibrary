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
        public void ExactSampleMerge()
        {
            using (var mat = Sys.Materializer())
            {
                var probe = CreateTestProbe();

                var graph = RunnableGraph.FromGraph(
                    GraphDsl.Create(builder =>
                    {
                        var primary = Source.From(
                            Enumerable.Range(0,100)
                            .Select(y => Mock.Of<ISyncData>(x => 
                            x.TimeStamp == 100 * y
                            && x.SampleIndex == y
                            )));

                        var secondary = Source.From(
                            Enumerable.Range(0,100)
                            .Select(y => Mock.Of<ISyncData>(x => 
                            x.TimeStamp == 100 * y
                            && x.SampleIndex == y
                            )));

                        var merger = builder.Add(new MergeClosestN<ISyncData>(2));

                        var sink = Sink.ActorRef<IImmutableList<ISyncData>>(probe, "completed");

                        builder.From(primary).To(merger.In(0));
                        builder.From(secondary).To(merger.In(1));
                        builder.From(merger.Out).To(sink);

                        return ClosedShape.Instance;
                    }));

                graph.Run(mat);

                var msgs = probe.ReceiveN(99, TimeSpan.FromSeconds(Debugger.IsAttached ? 300 : 3));
                msgs.Should().AllBeAssignableTo(typeof(IImmutableList<ISyncData>));

                var arrays = msgs.Cast<IImmutableList<ISyncData>>().ToList();
                var timestamps = arrays.Select(x => x.Select(y => y.TimeStamp).ToArray()).ToList();
                timestamps.Should().BeEquivalentTo(Enumerable.Range(0, 99).Select(x => new[] { x * 100, x * 100 }));

                var sampleIndices = arrays.Select(x => x.Select(y => y.SampleIndex).ToArray()).ToList();
                sampleIndices.Should().BeEquivalentTo(Enumerable.Range(0,99).Select(x => new[]{x,x}));
            }
        }

        [Fact]
        public void PrimaryTwiceRateOfSecondary()
        {
            using (var mat = Sys.Materializer())
            {
                var probe = CreateTestProbe();

                var graph = RunnableGraph.FromGraph(
                    GraphDsl.Create(builder =>
                    {
                        var primary = Source.From(
                            Enumerable.Range(0, 200)
                            .Select(y => Mock.Of<ISyncData>(x => 
                            x.TimeStamp == 50 * y
                            && x.SampleIndex == y
                            )));

                        var secondary = Source.From(
                            Enumerable.Range(0,100)
                            .Select(y => Mock.Of<ISyncData>(x => 
                            x.TimeStamp == 100 * y
                            && x.SampleIndex == y
                            )));

                        var merger = builder.Add(new MergeClosestN<ISyncData>(2));

                        var sink = Sink.ActorRef<IImmutableList<ISyncData>>(probe, "completed");

                        builder.From(primary).To(merger.In(0));
                        builder.From(secondary).To(merger.In(1));
                        builder.From(merger.Out).To(sink);

                        return ClosedShape.Instance;
                    }));

                graph.Run(mat);

                var msgs = probe.ReceiveN(97, TimeSpan.FromSeconds(Debugger.IsAttached ? 300 : 3));
                msgs.Should().AllBeAssignableTo(typeof(IImmutableList<ISyncData>));

                var arrays = msgs.Cast<IImmutableList<ISyncData>>().ToList();
                var timestamps = arrays.Select(x => x.Select(y => y.TimeStamp).ToArray()).ToList();
                timestamps.Should().BeEquivalentTo(Enumerable.Range(0, 97).Select(x => new[] { x * 100, x * 100 }));

                var sampleIndices = arrays.Select(x => x.Select(y => y.SampleIndex).ToArray()).ToList();
                sampleIndices.Should().BeEquivalentTo(Enumerable.Range(0,97).Select(x => new[]{2*x,x}));
            }
        }

        [Fact]
        public void Primary100AheadOfSecondary()
        {
            using (var mat = Sys.Materializer())
            {
                var probe = CreateTestProbe();

                var graph = RunnableGraph.FromGraph(
                    GraphDsl.Create(builder =>
                    {
                        var primary = Source.From(
                            Enumerable.Range(0, 200)
                            .Select(y => Mock.Of<ISyncData>(x => 
                            x.TimeStamp == 100 * y
                            && x.SampleIndex == y
                            )));

                        var secondary = Source.From(
                            Enumerable.Range(100, 100)
                            .Select(y => Mock.Of<ISyncData>(x => 
                            x.TimeStamp == 100 * y
                            && x.SampleIndex == y - 100
                            )));

                        var merger = builder.Add(new MergeClosestN<ISyncData>(2));

                        var sink = Sink.ActorRef<IImmutableList<ISyncData>>(probe, "completed");

                        builder.From(primary).To(merger.In(0));
                        builder.From(secondary).To(merger.In(1));
                        builder.From(merger.Out).To(sink);

                        return ClosedShape.Instance;
                    }));

                graph.Run(mat);

                var msgs = probe.ReceiveN(99, TimeSpan.FromSeconds(Debugger.IsAttached ? 300 : 3));
                msgs.Should().AllBeAssignableTo(typeof(IImmutableList<ISyncData>));

                var arrays = msgs.Cast<IImmutableList<ISyncData>>().ToList();
                var timestamps = arrays.Select(x => x.Select(y => y.TimeStamp).ToArray()).ToList();
                timestamps.Should().BeEquivalentTo(Enumerable.Range(0, 99).Select(x => new[] { 10000 + x * 100, 10000 + x * 100 }));

                var sampleIndices = arrays.Select(x => x.Select(y => y.SampleIndex).ToArray()).ToList();
                sampleIndices.Should().BeEquivalentTo(Enumerable.Range(0,99).Select(x => new[]{100 + x, x}));
            }
        }

        [Fact]
        public void ExactSampleFiveStreamMerge()
        {
            using (var mat = Sys.Materializer())
            {
                var probe = CreateTestProbe();

                var graph = RunnableGraph.FromGraph(
                    GraphDsl.Create(builder =>
                    {
                        var sources = Enumerable.Range(0, 5).Select(x =>
                            Source.From(
                                Enumerable.Range(0,100)
                                .Select(y => Mock.Of<ISyncData>(m =>
                                m.TimeStamp == 100 * y
                                && m.SampleIndex == y
                                )))).ToArray();

                        var merger = builder.Add(new MergeClosestN<ISyncData>(5));

                        var sink = Sink.ActorRef<IImmutableList<ISyncData>>(probe, "completed");

                        for (int i = 0; i < 5; i++)
                        {
                            builder.From(sources[i]).To(merger.In(i));
                        }
                        builder.From(merger.Out).To(sink);

                        return ClosedShape.Instance;
                    }));

                graph.Run(mat);

                var msgs = probe.ReceiveN(99, TimeSpan.FromSeconds(Debugger.IsAttached ? 300 : 3));
                msgs.Should().AllBeAssignableTo(typeof(IImmutableList<ISyncData>));

                var arrays = msgs.Cast<IImmutableList<ISyncData>>().ToList();
                var timestamps = arrays.Select(x => x.Select(y => y.TimeStamp).ToArray()).ToList();
                timestamps.Should().BeEquivalentTo(
                    Enumerable.Range(0, 99).Select(x => 
                    Enumerable.Range(0,5).Select(y => x * 100).ToArray()));

                var sampleIndices = arrays.Select(x => x.Select(y => y.SampleIndex).ToArray()).ToList();
                sampleIndices.Should().BeEquivalentTo(
                    Enumerable.Range(0,99).Select(x => 
                    Enumerable.Range(0,5).Select(y => x).ToArray()));
            }
        }
    }
}