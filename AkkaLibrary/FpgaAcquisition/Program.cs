using System;
using System.Collections.Generic;
using System.Diagnostics;
using Akka.Actor;
using Akka.Configuration;
using Akka.IO;
using Akka.Streams;
using AkkaLibrary.Common.Logging;
using AkkaLibrary.Common.Objects;
using Serilog;

namespace AkkaLibrary
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("FPGA Acquisition With Akka.");

            Log.Logger = LoggerFactory.Logger;

            var systemConfig = GetSystemConfig();

            using (var system = ActorSystem.Create("FpgaAcquisition", systemConfig))
            using (var materializer = system.Materializer())
            {
                var loggerActor = system.ActorOf<LoggerActor>("logger-writer");

                var fpgaConversion = system.ActorOf(FpgaConversionPluginActor.GetProps(loggerActor), "fpga-conversion-actor");

                var fpgaAcquisition = system.ActorOf<FpgaAcquisitionPluginActor>("fpga-plugin-actor");
                fpgaAcquisition.Tell(new FpgaPluginMessages.Configure(GetFpgaConfig(fpgaConversion)));


                var terminatedTask = system.WhenTerminated;
                terminatedTask.Wait();

                Log.Information($"System completed: {terminatedTask.IsCompletedSuccessfully}");
            }

            Log.CloseAndFlush();
        }

        private static FpgaAcquisitionConfiguration GetFpgaConfig(IActorRef outputTarget)
        {
            return new FpgaAcquisitionConfiguration
                {
                    IpAddress = "127.0.0.1",
                    Port = 10001,
                    ChannelList = new List<FpgaChannel>
                    {
                        //new FpgaChannel("Tachometer", ChannelType.UInt32),
                        new FpgaChannel("Analog1", ChannelType.Int16),
                        new FpgaChannel("Analog2", ChannelType.Int16),
                        new FpgaChannel("Analog3", ChannelType.Int16),
                        // new FpgaChannel("Analog4", ChannelType.Int16),
                        // new FpgaChannel("Analog5", ChannelType.Int16),
                        // new FpgaChannel("Analog6", ChannelType.Int16),

                        // new FpgaChannel("D1", ChannelType.Bool),
                        // new FpgaChannel("D2", ChannelType.Bool),
                        // new FpgaChannel("D3", ChannelType.Bool),
                        // new FpgaChannel("D4", ChannelType.Bool),
                        // new FpgaChannel("D5", ChannelType.Bool),
                        // new FpgaChannel("D6", ChannelType.Bool),
                        // new FpgaChannel("D7", ChannelType.Bool),
                        // new FpgaChannel("D8", ChannelType.Bool),
                    },
                    SamplesPerPacket = 32,
                    Delimiter = ByteString.CopyFrom(new byte[] { 0x00, 0x55, 0xAA, 0xff }),
                    MaxFrameLength = 1000,
                    OutputTarget = outputTarget,
                    RetryConnectionTimeout = TimeSpan.FromSeconds(3)
                };

        }

        private static Config GetSystemConfig()
        {
            return ConfigurationFactory.ParseString(@"akka
            {
                suppress-json-serializer-warning=true,
                loglevel=INFO,
                loggers=[""Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog""]
            }");
        }

        public sealed class RequestInputTarget { }
    }

    public class LoggerActor : ReceiveActor
    {
        private Stopwatch _stopwatch;
        private int sampleCount;
        private int _numSamples = 50000;
        private long _latestSampleIndex;
        private long _lastSampleIndex;

        public LoggerActor()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            Receive<string>(msg =>
            {
                Log.Information(msg);
            });

            Receive<FpgaSample>(msg =>
            {
                if(++sampleCount == _numSamples)
                {
                    sampleCount = 0;
                    var elapsed = _stopwatch.ElapsedMilliseconds;
                    Log.Information($"Sample Index Difference {msg.SampleIndex} - {_latestSampleIndex} = {msg.SampleIndex - _latestSampleIndex}.{Environment.NewLine}{elapsed/1000.0} seconds for {_numSamples} samples. Samples per second:{((double)_numSamples)/elapsed * 1000.0}.");
                    _latestSampleIndex = msg.SampleIndex;
                    _stopwatch.Restart();
                }
                
            });

            Receive<ChannelData<float>>(msg =>
            {
                if( (msg.SampleIndex - _lastSampleIndex) != 1)
                {
                    Log.Fatal($"Sample index incremented by {msg.SampleIndex - _lastSampleIndex}");
                }
                _lastSampleIndex = msg.SampleIndex;

                if(++sampleCount == _numSamples)
                {
                    sampleCount = 0;
                    var elapsed = _stopwatch.ElapsedMilliseconds;
                    Log.Information($"Sample Index Difference {msg.SampleIndex} - {_latestSampleIndex} = {msg.SampleIndex - _latestSampleIndex}.{Environment.NewLine}{elapsed/1000.0} seconds for {_numSamples} samples. Samples per second:{((double)_numSamples)/elapsed * 1000.0}.");
                    _latestSampleIndex = msg.SampleIndex;
                    _stopwatch.Restart();
                }
                
            });
        }
    }
}
