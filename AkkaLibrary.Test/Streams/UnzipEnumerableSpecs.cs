using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.TestKit.Xunit2;
using AkkaLibrary.Streams;
using AkkaLibrary.Streams.Graphs;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace AkkaLibrary.Test.Streams
{
    public class UnzipEnumerableSpecs : TestKit
    {
        [Fact]
        public void SingleOutlet()
        {
            using (var mat = Sys.Materializer())
            {
                var probe1 = CreateTestProbe();

                var graph = RunnableGraph.FromGraph(GraphDsl.Create(builder =>
                {
                    var source = Source.From(Enumerable.Range(1, 1).Select(x => new List<int>{x}));

                    var unzipper = builder.Add(new UnzipEnumerable<List<int>, int>(x => x.ToImmutableList(), 1));

                    var sink1 = Sink.ActorRef<int>(probe1, "completed");

                    builder.From(source).To(unzipper.In);

                    builder.From(unzipper.Out(0)).To(sink1);

                    return ClosedShape.Instance;
                }));

                graph.Run(mat);

                var msg = probe1.ExpectMsg<int>(TimeSpan.FromSeconds(3));
                msg.Should().Be(1);
            }
        }

        [Fact]
        public void DoubleOutlet()
        {
            using (var mat = Sys.Materializer())
            {
                var probe1 = CreateTestProbe();
                var probe2 = CreateTestProbe();

                var graph = RunnableGraph.FromGraph(GraphDsl.Create(builder =>
                {
                    var source = Source.From(Enumerable.Range(1, 1).Select(x => Enumerable.Range(1,2).ToList()));

                    var unzipper = builder.Add(new UnzipEnumerable<List<int>, int>(x => x.ToImmutableList(), 2));

                    var sink1 = Sink.ActorRef<int>(probe1, "completed");
                    var sink2 = Sink.ActorRef<int>(probe2, "completed");

                    builder.From(source).To(unzipper.In);

                    builder.From(unzipper.Out(0)).To(sink1);
                    builder.From(unzipper.Out(1)).To(sink2);

                    return ClosedShape.Instance;
                }));

                graph.Run(mat);

                var msg = probe1.ExpectMsg<int>(TimeSpan.FromSeconds(3));
                msg.Should().Be(1);

                msg = probe2.ExpectMsg<int>(TimeSpan.FromSeconds(3));
                msg.Should().Be(2);
            }
        }

        [Property]
        public Property UnzipEnumerableProps()
        {
            return Prop.ForAll<PositiveInt>(value =>
            {
                using (var mat = Sys.Materializer())
                {
                    var probes = Enumerable.Range(0,value.Get).Select(x => CreateTestProbe()).ToList();

                    var graph = RunnableGraph.FromGraph(GraphDsl.Create(builder =>
                    {
                        var source = Source.From(Enumerable.Range(1, 1).Select(x => Enumerable.Range(0, value.Get).ToList()));

                        var unzipper = builder.Add(new UnzipEnumerable<List<int>, int>(x => x.ToImmutableList(), value.Get));

                        var sinks = probes.Select(p => Sink.ActorRef<int>(p, "completed")).ToList();

                        for (int i = 0; i < value.Get; i++)
                        {
                            builder.From(unzipper.Out(i)).To(sinks[i]);
                        }

                        builder.From(source).To(unzipper.In);

                        return ClosedShape.Instance;
                    }));

                    graph.Run(mat);

                    for (int i = 0; i < value.Get; i++)
                    {
                        var msg = probes[i].ExpectMsg<int>(TimeSpan.FromSeconds(3));
                        msg.Should().Be(i);
                    }
                }
            });
        }
    }
}