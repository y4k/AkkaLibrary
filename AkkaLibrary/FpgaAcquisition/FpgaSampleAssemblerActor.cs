using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Dsl;
using AkkaLibrary.Common.Utilities;
using Serilog;

namespace AkkaLibrary
{
    public class FpgaSampleAssemblerActor : ReceiveActor
    {
        public FpgaSampleAssemblerActor(IEnumerable<FpgaChannel> channelList, int samplesPerPacket, IActorRef outputTarget)
        {
            _outputTarget = outputTarget;
            _channelList = channelList.ToArray();

            _digitalChannelNames = _channelList.Where(x => x.DataType == ChannelType.Bool)
                                                .Select(x => x.ChannelName).ToList();

            _samplesPerPacket = samplesPerPacket;

            _sampleLength = CalculateSampleLength(_channelList);

            _assemblerFlow = CreateAssemblerLogic(_samplesPerPacket, _sampleLength, _outputTarget);

            Ready();
        }

        private ISourceQueueWithComplete<ByteString> CreateAssemblerLogic(int samplesPerPacket, int sampleLength, IActorRef outputTarget)
        {
            var assemblerLogic = Flow.Create<ByteString>()
                                .SelectMany(bs =>
                                {
                                    var sliceList = new List<ByteString>();
                                    for (int i = 0; i < (samplesPerPacket * sampleLength); i += sampleLength)
                                    {
                                        var stream = new MemoryStream();
                                        sliceList.Add(bs.Slice(i, sampleLength));
                                    }
                                    return sliceList;
                                })
                                .Select(bytestring => (index: ++_sampleIndex, bytes: bytestring))
                                .SelectAsync(4, tup => Task.Run(() => AssembleSample(tup.bytes, tup.index)))
                                .To(Sink.ActorRef<FpgaSample>(outputTarget, new FpgaPluginMessages.StreamComplete()));

            var source = Source.Queue<ByteString>(10000, OverflowStrategy.Backpressure);

            return source.ToMaterialized(assemblerLogic, Keep.Left).Run(Context.System.Materializer());
        }

        private int CalculateSampleLength(IReadOnlyList<FpgaChannel> channelList)
        {
            var sampleLength = 0;

            foreach (var item in channelList)
            {
                switch (item.DataType)
                {
                    case ChannelType.UInt32:
                        sampleLength += 32;
                        break;
                    case ChannelType.Int16:
                        sampleLength += 16;
                        break;
                    case ChannelType.Int24:
                        sampleLength += 24;
                        break;
                    case ChannelType.Int32:
                        sampleLength += 32;
                        break;
                    case ChannelType.Float:
                        sampleLength += 32;
                        break;
                    case ChannelType.Double:
                        sampleLength += 64;
                        break;
                    case ChannelType.Bool:
                        sampleLength += 1;
                        break;
                    default:
                        break;
                }
            }

            return sampleLength/8;
        }

        private void Ready()
        {
            Receive<ByteString>(msg =>
            {
                _assemblerFlow.OfferAsync(msg).PipeTo(Self);
            });

            Receive<IQueueOfferResult>(enqueueTask =>
            {
                enqueueTask.Match()
                        .With<QueueOfferResult.Enqueued>(msg => ++_messagesEnqueued)
                        .With<QueueOfferResult.Dropped>(msg => Log.Warning("FPGA assembler actor dropped a message. Total dropped:{0}", ++_messagesDropped))
                        .With<QueueOfferResult.Failure>(msg => throw msg.Cause)
                        .With<QueueOfferResult.QueueClosed>(msg => throw new Exception("The stream queue was closed."));
            });
        }

        private FpgaSample AssembleSample(ByteString bytes, int sampleIndex)
        {
            using(var stream = new MemoryStream(bytes.ToArray()))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var uint32s = new List<(string, UInt32)>();
                    var int16s = new List<(string, Int16)>();
                    var int24s = new List<(string, Int24)>();
                    var int32s = new List<(string, Int32)>();
                    var floats = new List<(string, float)>();
                    var doubles = new List<(string, Double)>();
                    var bools = new List<(string, bool)>();

                    var boolsToRead = 0;
                    foreach (var channel in _channelList)
                    {
                        switch (channel.DataType)
                        {
                            case ChannelType.UInt32:
                                uint32s.Add((channel.ChannelName, reader.ReadUInt32()));
                                break;
                            case ChannelType.Int16:
                                int16s.Add((channel.ChannelName, reader.ReadInt16()));
                                break;
                            case ChannelType.Int24:
                                int24s.Add((channel.ChannelName, reader.ReadInt24()));
                                break;
                            case ChannelType.Int32:
                                int32s.Add((channel.ChannelName, reader.ReadInt32()));
                                break;
                            case ChannelType.Float:
                                floats.Add((channel.ChannelName, reader.ReadSingle()));
                                break;
                            case ChannelType.Double:
                                doubles.Add((channel.ChannelName, reader.ReadDouble()));
                                break;
                            case ChannelType.Bool:
                                //Add the bools to a separate array and handle afterwards.
                                ++boolsToRead;
                                break;
                            default:
                                break;
                        }
                    }

                    //Assemble the bools
                    if (boolsToRead > 0)
                    {
                        var digitalBytes = reader.ReadBytes(boolsToRead / 8);
                        var bits = new BitArray(digitalBytes);

                        for (var i = 0; i < bits.Length; i++)
                        {
                            bools.Add((_digitalChannelNames[i], bits[i]));
                        }
                    }

                    var sample = new FpgaSample(0, 0, 0, false, sampleIndex,
                                        uint32s, 
                                        int16s,
                                        int24s,
                                        int32s,
                                        floats,
                                        doubles,
                                        bools
                                        );

                    return sample;
                }
            }
        }

        public static Props GetProps(IEnumerable<FpgaChannel> channels, int samplesPerPacket, IActorRef outputTarget) => Props.Create(() => new FpgaSampleAssemblerActor(channels, samplesPerPacket, outputTarget));

        private int _samplesPerPacket;
        private IActorRef _outputTarget;
        private IReadOnlyList<FpgaChannel> _channelList;
        private List<string> _digitalChannelNames;
        private int _sampleLength;
        private ISourceQueueWithComplete<ByteString> _assemblerFlow;
        private int _messagesEnqueued;
        private int _messagesDropped;
        private int _sampleIndex;
    }
}