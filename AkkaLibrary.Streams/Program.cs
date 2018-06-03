using System;
using System.Collections.Generic;
using System.IO;
using Akka;
using Akka.Actor;
using Akka.Configuration;
using Akka.Streams;
using Akka.Streams.Dsl;
using AkkaLibrary.Common.Interfaces;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace DataSynchronisation
{
    partial class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = LoggerFactory.Logger;

            var logSwitch = LoggerFactory.LoggingSwitch;

            var config = ConfigurationFactory.ParseString(File.ReadAllText("config.hocon"));

            logSwitch.MinimumLevel = LogEventLevel.Debug;

            using (var system = ActorSystem.Create("System", config))
            using(var materialiser = system.Materializer())
            {
                Log.Information("System Started");
                // Create actors.

                var loggingActor = system.ActorOf<LoggingActor>("Logger");

                // Create streams.
                var generator = Source.From<ISyncData>(GenerateData());

                var decimator = new SpatialDecimator<ISyncData>(300, 1);

                var flow = Flow.Create<ISyncData>().Via(decimator);

                var sink = Sink.ActorRef<ISyncData>(loggingActor, PoisonPill.Instance);

                var queue = generator.Via(flow).RunWith(sink, materialiser);

                // Close system logic.
                Console.CancelKeyPress += (obj, a) =>
                {
                    system.Terminate().Wait(TimeSpan.FromSeconds(3));
                };
                system.WhenTerminated.Wait();
                Log.Information("System Stopped");
                Log.CloseAndFlush();
            }
        }

        private static IEnumerable<ISyncData> GenerateData()
        {
            uint tachoCount = 0;
            for (int i = 0; i < 100; i++)
            {
                yield return new TimedObject
                {
                    TachometerCount = tachoCount += 100
                };
            }
        }
    }
}