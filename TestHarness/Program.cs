using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Akka.Streams;
using Akka.Configuration;
using Akka.Streams.Dsl;
using AkkaLibrary;
using AkkaLibrary.Common.Objects;
using AkkaLibrary.Common.Configuration;

namespace TestHarness
{
    class Program
    {
        private static Config config = CommonConfigs.BasicConfig().WithFallback(CommonConfigs.CreateLoggingConfig(new SerilogConfig()));

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .CreateLogger();

            var channelAdjusterConfigs = new List<ChannelAdjusterConfig>
            {
                new ChannelAdjusterConfig("ChannelOne",0,0,-2,FilterOption.PassThrough),
                new ChannelAdjusterConfig("ChannelTwo",0,0,-1,FilterOption.PassThrough),
            };

            var firstData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 1, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", 2, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                0,0,0,false,0
            );

            var secondData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 4, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", 5, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                0,0,0,false,0
            );

            var thirdData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 40, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", 50, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                0,0,0,false,0
            );

            using (var system = ActorSystem.Create("TestSystem", config))
            using (var mat = system.Materializer())
            {
                var logger = system.ActorOf<LogActor>("log-actor");

                var adjuster = system.ActorOf(ChannelAdjuster.GetProps(channelAdjusterConfigs, logger));

                adjuster.Tell(firstData);
                adjuster.Tell(secondData);
                adjuster.Tell(thirdData);

                Console.ReadLine();
            }
        }
    }

    internal static class ExtractionTestSource
    {
        public static IEnumerable<ExtractionTestObject> Generate()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return new ExtractionTestObject
                {
                    StringTest = Guid.NewGuid().ToString(),
                    IntTest = i,
                    DoubleTest = i * 100.0,
                    IntArrayTest = Enumerable.Range(i, 10).ToArray(),
                    FloatListTest = Enumerable.Range(i, 10).Select(x => (float)(x * 1000.0f)).ToList(),
                    Nested = new NestedClass
                    {
                        NestedInt = i + 1,
                        NestedString = Guid.NewGuid().ToString(),
                        NestedArray = new int[] { 5, 7, 9 },
                        NestedList = new string[] { "5", "7", "9" }.ToList(),
                        NestedDict = new Dictionary<string, int>
                        {
                            {"Ten", 10},
                            {"Twenty", 20},
                            {"Thirty", 30},
                        },
                        NestedClassList = Enumerable.Range(10, 5).Select(x => new AccessibleClass { Key = $"Key{x}", Value = $"Value{x}" }).ToList()
                    }
                };
            }
        }
    }

    public class ExtractionTestObject
    {
        public string StringTest { get; set; }
        public int IntTest { get; set; }
        public double DoubleTest { get; set; }
        public int[] IntArrayTest { get; set; }
        public List<float> FloatListTest { get; set; }

        public NestedClass Nested { get; set; }
    }

    public class NestedClass
    {
        public int NestedInt { get; set; }
        public string NestedString { get; set; }
        public int[] NestedArray { get; set; }
        public List<string> NestedList { get; set; }
        public Dictionary<string, int> NestedDict { get; set; }
        public List<AccessibleClass> NestedClassList { get; set; }
    }

    public class AccessibleClass
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class AssignmentTestObject
    {
        public string NewName { get; set; }

        public AccessibleClass NestedClass { get; set; }
    }
}