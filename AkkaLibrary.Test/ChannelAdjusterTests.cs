using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using AkkaLibrary.Common.Objects;
using FluentAssertions;
using Xunit;

namespace AkkaLibrary.Test
{
    public class ChannelAdjusterTests : TestKit
    {
        [Fact]
        public void GraphCreation()
        {
            var configs = new List<ChannelAdjusterConfig>
            {
                new ChannelAdjusterConfig("ChannelOne",0,0,0,FilterOption.Filter)
            };

            var sample = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne",1, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                0,0,0,false,0
            );

            var ca = Sys.ActorOf(Props.Create(() => new ChannelAdjuster(configs,TestActor)));

            var firstData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 1, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                0,0,0,false,0
            );

            ca.Tell(firstData);

            var result = ExpectMsg<ChannelData<float>>(TimeSpan.FromSeconds(3));
            result.Analogs.Should().HaveCount(1);
            result.Digitals.Should().HaveCount(0);
            result.Analogs.First().Value.Should().Be(0);
        }

        [Fact]
        public void TwoChannelPassThrough()
        {
            var configs = new List<ChannelAdjusterConfig>
            {
                new ChannelAdjusterConfig("ChannelOne",0,0,0,FilterOption.PassThrough),
                new ChannelAdjusterConfig("ChannelTwo",0,0,0,FilterOption.PassThrough)
            };

            var ca = Sys.ActorOf(Props.Create(() => new ChannelAdjuster(configs,TestActor)));

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

            ca.Tell(firstData);

            var result = ExpectMsg<ChannelData<float>>(TimeSpan.FromSeconds(3));
            result.Analogs.Should().HaveCount(2);
            result.Digitals.Should().HaveCount(0);
            result.Analogs.Select(x => x.Name).Should().BeEquivalentTo(new []{"ChannelOne","ChannelTwo"});
        }

        [Fact]
        public void TwoChannelPassThroughReordered()
        {
            var configs = new List<ChannelAdjusterConfig>
            {
                new ChannelAdjusterConfig("ChannelTwo",0,0,0,FilterOption.PassThrough),
                new ChannelAdjusterConfig("ChannelOne",0,0,0,FilterOption.PassThrough),
            };

            var ca = Sys.ActorOf(Props.Create(() => new ChannelAdjuster(configs,TestActor)));

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

            ca.Tell(firstData);

            var result = ExpectMsg<ChannelData<float>>(TimeSpan.FromSeconds(3));
            result.Analogs.Should().HaveCount(2);
            result.Digitals.Should().HaveCount(0);
            result.Analogs.Select(x => x.Name).Should().BeEquivalentTo(new []{"ChannelTwo","ChannelOne"});
        }

        [Fact]
        public void TemporalOffsetByOneTwoSamples()
        {
            var configs = new List<ChannelAdjusterConfig>
            {
                new ChannelAdjusterConfig("ChannelOne",0,0,1,FilterOption.PassThrough),
                new ChannelAdjusterConfig("ChannelTwo",0,0,0,FilterOption.PassThrough),
            };

            var ca = Sys.ActorOf(Props.Create(() => new ChannelAdjuster(configs,TestActor)));            

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

            ca.Tell(firstData);
            ca.Tell(secondData);

            var result = ExpectMsg<ChannelData<float>>(TimeSpan.FromSeconds(15));

            result.Analogs.Should().HaveCount(2);
            result.Digitals.Should().HaveCount(0);
            result.Analogs.Select(x => x.Name).Should().BeEquivalentTo(new []{"ChannelOne", "ChannelTwo"});

            result.Analogs[0].Value.Should().Be(1);
            result.Analogs[1].Value.Should().Be(5);
        }

        [Fact]
        public void TemporalOffsetByNegativeTwoSamples()
        {
            var configs = new List<ChannelAdjusterConfig>
            {
                new ChannelAdjusterConfig("ChannelOne",0,0,0,FilterOption.PassThrough),
                new ChannelAdjusterConfig("ChannelTwo",0,0,-1,FilterOption.PassThrough),
            };

            var ca = Sys.ActorOf(Props.Create(() => new ChannelAdjuster(configs,TestActor)));

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

            ca.Tell(firstData);
            ca.Tell(secondData);

            var result = ExpectMsg<ChannelData<float>>(TimeSpan.FromSeconds(5));
            result.Analogs.Should().HaveCount(2);
            result.Digitals.Should().HaveCount(0);
            result.Analogs.Select(x => x.Name).Should().BeEquivalentTo(new []{"ChannelOne", "ChannelTwo"});

            result.Analogs[0].Value.Should().Be(1);
            result.Analogs[1].Value.Should().Be(5);
        }

        [Fact]
        public void TemporalOffsetByNegativeAndPositiveThreeSamples()
        {
            var configs = new List<ChannelAdjusterConfig>
            {
                new ChannelAdjusterConfig("ChannelOne",0,0,1,FilterOption.PassThrough),
                new ChannelAdjusterConfig("ChannelTwo",0,0,-1,FilterOption.PassThrough),
            };

            var ca = Sys.ActorOf(Props.Create(() => new ChannelAdjuster(configs,TestActor)));

            var firstData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 1, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -1, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                0,0,0,false,0
            );

            var secondData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 2, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -2, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                0,0,0,false,0
            );

            var thirdData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 3, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -3, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                0,0,0,false,0
            );

            ca.Tell(firstData);
            ca.Tell(secondData);
            ca.Tell(thirdData);

            var result = ExpectMsg<ChannelData<float>>(TimeSpan.FromSeconds(5));
            result.Analogs.Should().HaveCount(2);
            result.Digitals.Should().HaveCount(0);
            result.Analogs.Select(x => x.Name).Should().BeEquivalentTo(new []{"ChannelOne", "ChannelTwo"});

            result.Analogs[0].Value.Should().Be(1);
            result.Analogs[1].Value.Should().Be(-3);
        }

        [Fact]
        public void TemporalOffsetSyncDataZerothMiddleOfThreeSamples()
        {
            var configs = new List<ChannelAdjusterConfig>
            {
                new ChannelAdjusterConfig("ChannelOne",0,0,1,FilterOption.PassThrough),
                new ChannelAdjusterConfig("ChannelTwo",0,0,-1,FilterOption.PassThrough),
            };

            var ca = Sys.ActorOf(Props.Create(() => new ChannelAdjuster(configs,TestActor)));

            var firstData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 1, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -1, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                1,1,1,false,1
            );

            var secondData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 2, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -2, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                2,2,2,false,2
            );

            var thirdData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 3, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -3, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                3,3,3,false,3
            );

            ca.Tell(firstData);
            ca.Tell(secondData);
            ca.Tell(thirdData);

            var result = ExpectMsg<ChannelData<float>>(TimeSpan.FromSeconds(5));
            result.Analogs.Should().HaveCount(2);
            result.Digitals.Should().HaveCount(0);
            result.Analogs.Select(x => x.Name).Should().BeEquivalentTo(new []{"ChannelOne", "ChannelTwo"});

            result.Analogs[0].Value.Should().Be(1);
            result.Analogs[1].Value.Should().Be(-3);

            result.MasterSyncIncrement.Should().Be(2);
            result.TimeStamp.Should().Be(2);
            result.TachometerCount.Should().Be(2);
            result.SampleIndex.Should().Be(2);
        }

        [Fact]
        public void TemporalOffsetSyncDataZerothLowerOfThreeSamples()
        {
            var configs = new List<ChannelAdjusterConfig>
            {
                new ChannelAdjusterConfig("ChannelOne",0,0,1,FilterOption.PassThrough),
                new ChannelAdjusterConfig("ChannelTwo",0,0,2,FilterOption.PassThrough),
            };

            var ca = Sys.ActorOf(Props.Create(() => new ChannelAdjuster(configs,TestActor)));

            var firstData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 1, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -1, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                1,1,1,false,1
            );

            var secondData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 2, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -2, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                2,2,2,false,2
            );

            var thirdData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 3, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -3, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                3,3,3,false,3
            );

            ca.Tell(firstData);
            ca.Tell(secondData);
            ca.Tell(thirdData);

            var result = ExpectMsg<ChannelData<float>>(TimeSpan.FromSeconds(5));
            result.Analogs.Should().HaveCount(2);
            result.Digitals.Should().HaveCount(0);
            result.Analogs.Select(x => x.Name).Should().BeEquivalentTo(new []{"ChannelOne", "ChannelTwo"});

            result.Analogs[0].Value.Should().Be(2);
            result.Analogs[1].Value.Should().Be(-1);

            result.MasterSyncIncrement.Should().Be(3);
            result.TimeStamp.Should().Be(3);
            result.TachometerCount.Should().Be(3);
            result.SampleIndex.Should().Be(3);
        }

        [Fact]
        public void TemporalOffsetSyncDataZerothHigestOfThreeSamples()
        {
            var configs = new List<ChannelAdjusterConfig>
            {
                new ChannelAdjusterConfig("ChannelOne",0,0,-1,FilterOption.PassThrough),
                new ChannelAdjusterConfig("ChannelTwo",0,0,-2,FilterOption.PassThrough),
            };

            var ca = Sys.ActorOf(Props.Create(() => new ChannelAdjuster(configs,TestActor)));

            var firstData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 1, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -1, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                1,1,1,false,1
            );

            var secondData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 2, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -2, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                2,2,2,false,2
            );

            var thirdData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 3, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -3, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                3,3,3,false,3
            );

            ca.Tell(firstData);
            ca.Tell(secondData);
            ca.Tell(thirdData);

            var result = ExpectMsg<ChannelData<float>>(TimeSpan.FromSeconds(5));
            result.Analogs.Should().HaveCount(2);
            result.Digitals.Count.Should().Be(0);
            result.Analogs.Select(x => x.Name).Should().BeEquivalentTo(new []{"ChannelOne", "ChannelTwo"});

            result.Analogs[0].Value.Should().Be(2);
            result.Analogs[1].Value.Should().Be(-3);

            result.MasterSyncIncrement.Should().Be(1);
            result.TimeStamp.Should().Be(1);
            result.TachometerCount.Should().Be(1);
            result.SampleIndex.Should().Be(1);
        }


        [Fact]
        public void TemporalOffsetSyncDataZerothSixSamples()
        {
            var configs = new List<ChannelAdjusterConfig>
            {
                new ChannelAdjusterConfig("ChannelOne",0,0,3,FilterOption.PassThrough),
                new ChannelAdjusterConfig("ChannelTwo",0,0,5,FilterOption.PassThrough),
            };

            var ca = Sys.ActorOf(Props.Create(() => new ChannelAdjuster(configs,TestActor)));

            var firstData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 1, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -1, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                1,1,1,false,1
            );

            var secondData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 2, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -2, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                2,2,2,false,2
            );

            var thirdData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 3, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -3, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                3,3,3,false,3
            );

            var fourthData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 4, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -4, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                4,4,4,false,4
            );

            var fifthData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 5, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -5, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                5,5,5,false,5
            );

            var sixthData = new ChannelData<float>
            (
                new List<DataChannel<float>>
                {
                    new DataChannel<float>("ChannelOne", 6, Unit.Metres),
                    new DataChannel<float>("ChannelTwo", -6, Unit.Metres)
                },
                new List<DataChannel<bool>>(),
                6,6,6,false,6
            );

            ca.Tell(firstData);
            ca.Tell(secondData);
            ca.Tell(thirdData);
            ca.Tell(fourthData);
            ca.Tell(fifthData);
            ca.Tell(sixthData);

            var result = ExpectMsg<ChannelData<float>>(TimeSpan.FromSeconds(5));
            result.Analogs.Should().HaveCount(2);
            result.Digitals.Should().HaveCount(0);
            result.Analogs.Select(x => x.Name).Should().BeEquivalentTo(new []{"ChannelOne", "ChannelTwo"});

            result.Analogs[0].Value.Should().Be(3);
            result.Analogs[1].Value.Should().Be(-1);

            result.MasterSyncIncrement.Should().Be(6);
            result.TimeStamp.Should().Be(6);
            result.TachometerCount.Should().Be(6);
            result.SampleIndex.Should().Be(6);
        }
    }
}