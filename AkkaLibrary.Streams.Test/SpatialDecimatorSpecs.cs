using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DataSynchronisation.Test
{
    public class SpatialDecimatorSpecs : TestKit
    {
        private ActorMaterializer Materializer { get; }

        private static Config TestConfig => ConfigurationFactory.ParseString("akka.loglevel=DEBUG, akka.loggers=[\"Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog\"]");

        public SpatialDecimatorSpecs(ITestOutputHelper helper) : base(TestConfig, "test-system", helper)
        {
            var settings = ActorMaterializerSettings.Create(Sys).WithInputBuffer(2, 2);
            Materializer = ActorMaterializer.Create(Sys, settings);
            Serilog.Log.Logger = LoggerFactory.Logger;
        }

        [Fact]
        public void HalfSpatialDistance()
        {
            var source = Source.From(
                Enumerable
                    .Range(0, 100)
                    .Select(x => new TimedObject { TachometerCount = x * 50 })
                    );

            var decimator = new SpatialDecimator<TimedObject>(100, 1);

            var probe = CreateTestProbe();

            source.Via(decimator).RunWith(Sink.ActorRef<TimedObject>(probe, new object()), Materializer);

            var actual = probe.ReceiveN(50);

            actual.Should().AllBeAssignableTo<TimedObject>();

            var msgs = actual.Cast<TimedObject>();

            msgs.Should().BeInAscendingOrder(x => x.TachometerCount);

            msgs.Select(x => x.TachometerCount).Should().BeEquivalentTo(Enumerable.Range(0, 50).Select(x => x * 100));
        }

        [Fact]
        public void ZeroSpatialDecimation()
        {
            var source = Source.From(
                Enumerable
                    .Range(0, 100)
                    .Select(x => new TimedObject { TachometerCount = x * 50 })
                    );

            var decimator = new SpatialDecimator<TimedObject>(100, 0);

            var probe = CreateTestProbe();

            source.Via(decimator).RunWith(Sink.ActorRef<TimedObject>(probe, new object()), Materializer);

            var actual = probe.ReceiveN(100);

            actual.Should().AllBeAssignableTo<TimedObject>();

            var msgs = actual.Cast<TimedObject>();

            msgs.Should().BeInAscendingOrder(x => x.TachometerCount);

            msgs.Select(x => x.TachometerCount).Should().BeEquivalentTo(Enumerable.Range(0, 100).Select(x => x * 50));
        }

        [Fact]
        public void RepeatedTachoFilteredOut()
        {
            var source = Source.From(
                Enumerable
                    .Range(0, 100)
                    .Select(x => (x % 2 == 0) ? x : x - 1)
                    .Select(x => new TimedObject { TachometerCount = x * 50})
                    );

            var decimator = new SpatialDecimator<TimedObject>(100, 0);

            var probe = CreateTestProbe();

            source.Via(decimator).RunWith(Sink.ActorRef<TimedObject>(probe, new object()), Materializer);

            var actual = probe.ReceiveN(50, Debugger.IsAttached ? TimeSpan.FromSeconds(300) : TimeSpan.FromSeconds(3));

            actual.Should().AllBeAssignableTo<TimedObject>();

            var msgs = actual.Cast<TimedObject>();

            msgs.Should().BeInAscendingOrder(x => x.TachometerCount);

            msgs.Select(x => x.TachometerCount).Should().BeEquivalentTo(Enumerable.Range(0, 50).Select(x => x * 100));
        }
    }
}
