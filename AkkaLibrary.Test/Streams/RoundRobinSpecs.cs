using System;
using System.Diagnostics;
using System.Linq;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.TestKit.Xunit2;
using AkkaLibrary.Streams.Graphs;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace AkkaLibrary.Test.Streams
{
    public class RoundRobinSpecs : TestKit
    {
        [Fact]
        public void SingleOutlet()
        {
            using (var mat = Sys.Materializer())
            {
                var probe1 = CreateTestProbe();

                var graph = RunnableGraph.FromGraph(GraphDsl.Create(builder =>
                {
                    var source = Source.From(Enumerable.Range(1, 100));

                    var roundRobin = builder.Add(new RoundRobinFanOut<int>(1));

                    var sink1 = Sink.ActorRef<int>(probe1, "completed");

                    builder.From(source).To(roundRobin.In);

                    builder.From(roundRobin.Out(0)).To(sink1);

                    return ClosedShape.Instance;
                }));

                graph.Run(mat);

                var msgs = probe1.ExpectMsgAllOf<int>(TimeSpan.FromSeconds(3), Enumerable.Range(1,100).ToArray());
                msgs.Should().BeEquivalentTo(Enumerable.Range(1, 100));
            }
        }

        [Fact]
        public void TwoOutlets()
        {
            using (var mat = Sys.Materializer())
            {
                var probe1 = CreateTestProbe();
                var probe2 = CreateTestProbe();

                var graph = RunnableGraph.FromGraph(GraphDsl.Create(builder =>
                {
                    var source = Source.From(Enumerable.Range(1, 100));

                    var roundRobin = builder.Add(new RoundRobinFanOut<int>(2));

                    var sink1 = Sink.ActorRef<int>(probe1, "completed");
                    var sink2 = Sink.ActorRef<int>(probe2, "completed");

                    builder.From(source).To(roundRobin.In);

                    builder.From(roundRobin.Out(0)).To(sink1);
                    builder.From(roundRobin.Out(1)).To(sink2);

                    return ClosedShape.Instance;
                }));

                graph.Run(mat);

                var msg1 = probe1.ExpectMsgAllOf<int>(TimeSpan.FromSeconds(3), Enumerable.Range(1,100).Where(x => x % 2 == 1).ToArray());
                var msg2 = probe2.ExpectMsgAllOf<int>(TimeSpan.FromSeconds(3), Enumerable.Range(1,100).Where(x => x % 2 == 0).ToArray());
            }
        }

        [Property(Verbose = true)]
        public Property RoundRobinDistributerSpecs()
        {
            return Prop.ForAll<PositiveInt>(value =>
            {
                var probes = Enumerable.Range(0, value.Get).Select(_ => CreateTestProbe()).ToList();
                var sinks = probes.Select(p => Sink.ActorRef<int>(p, "completed")).ToList();

                using (var mat = Sys.Materializer())
                {
                    var graph = RunnableGraph.FromGraph(GraphDsl.Create(builder =>
                    {
                        var source = Source.From(Enumerable.Range(0, value.Get));

                        var roundRobin = builder.Add(new RoundRobinFanOut<int>(value.Get));

                        builder.From(source).To(roundRobin.In);

                        for (int i = 0; i < value.Get; i++)
                        {
                            builder.From(roundRobin.Out(i)).To(sinks[i]);
                        }

                        return ClosedShape.Instance;
                    }));

                    graph.Run(mat);

                    for (int i = 0; i < value.Get; i++)
                    {
                        var msg = probes[i].ExpectMsg<int>(TimeSpan.FromSeconds(3));
                        msg.Should().Be(i);
                        var completeMsg = probes[i].ExpectMsg<string>(TimeSpan.FromSeconds(3));
                        completeMsg.Should().Be("completed");
                    }
                }
            });
        }

        [Property(Verbose = true)]
        public Property RoundRobinMergerProps()
        {
            return Prop.ForAll<PositiveInt>(value =>
            {
                var probe = CreateTestProbe();
                var sink = Sink.ActorRef<int>(probe, "completed");
                var sources = Enumerable.Range(0,value.Get).Select(x => Source.Single(x)).ToList();

                using (var mat = Sys.Materializer())
                {
                    var graph = RunnableGraph.FromGraph(GraphDsl.Create(builder =>
                    {
                        var roundRobin = builder.Add(new RoundRobinFanIn<int>(value.Get));

                        builder.From(roundRobin).To(sink);

                        for (int i = 0; i < value.Get; i++)
                        {
                            builder.To(roundRobin.In(i)).From(sources[i]);
                        }

                        return ClosedShape.Instance;
                    }));

                    graph.Run(mat);

                    var msgs = probe.ReceiveN(value.Get, Debugger.IsAttached ? TimeSpan.FromSeconds(300) : TimeSpan.FromSeconds(3));
                    msgs.Should().BeEquivalentTo(Enumerable.Range(0,value.Get));
                }
            });
        }

        [Property(Verbose = true)]
        public Property RoundRobinFanInAndOut()
        {
            return Prop.ForAll<PositiveInt>(value =>
            {
                var probe = CreateTestProbe();
                var sink = Sink.ActorRef<int>(probe, "completed");
                var source = Source.From(Enumerable.Range(0,value.Get));

                using (var mat = Sys.Materializer())
                {
                    var graph = RunnableGraph.FromGraph(GraphDsl.Create(builder =>
                    {
                        var distributer = builder.Add(new RoundRobinFanOut<int>(value.Get));
                        var merger = builder.Add(new RoundRobinFanIn<int>(value.Get));

                        builder.From(source).To(distributer.In);

                        for (int i = 0; i < value.Get; i++)
                        {
                            builder.From(distributer.Out(i)).To(merger.In(i));
                        }

                        builder.From(merger.Out).To(sink);

                        return ClosedShape.Instance;
                    }));

                    graph.Run(mat);

                    var msgs = probe.ReceiveN(value.Get, Debugger.IsAttached ? TimeSpan.FromSeconds(300) : TimeSpan.FromSeconds(3));
                    msgs.Should().BeEquivalentTo(Enumerable.Range(0,value.Get));
                }
            });
        }
    }
}