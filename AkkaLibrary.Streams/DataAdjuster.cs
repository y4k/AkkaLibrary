using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using AkkaLibrary.Common.Interfaces;
using AkkaLibrary.Common.Objects;
using AkkaLibrary.Streams.GraphStages;

namespace AkkaLibrary.Streams
{
    public class ChannelAdjusterConfig
    {
        public string Name { get; }

        public float Scale { get; }

        public float Offset { get; }

        public FilterOption Option { get; }

        public int TemporalOffset { get; }

        public ChannelAdjusterConfig(string name, float scale, float offset, int temporalOffset, FilterOption option)
        {
            Name = name;
            Scale = scale;
            Offset = offset;
            Option = option;
            TemporalOffset = temporalOffset;
        }
    }

    public enum FilterOption
    {
        NotSet,
        PassThrough,
        CreateDigitals,
        Filter
    }

    public static class DataAdjusterFactory
    {
        private static IEnumerable<int> GetIndices(ChannelData<float> sample, IEnumerable<ChannelAdjusterConfig> configs)
        {
            return configs.Select(cfg => sample.Analogs.Select((x, i) => (index: i, data: x)).First(pair => pair.data.Name == cfg.Name).index);
        }

        private static RunnableGraph<ISourceQueueWithComplete<ChannelData<float>>> CreateGraph(IActorRef target, List<ChannelAdjusterConfig> configs, ChannelData<float> sample)
        {
            /*
                Digital Merger is only necessary when there are additional digitals created and the same goes for the
                Broadcast following the Analog Splitter. A broadcast is only required when the analog channel is produces
                the additional digitals. Otherwise the analog is pushed straight to the merger
                +---------------+--------------+-----------------------------------------------------------------------+
                |               |              |                     SyncData                           |              |
                |               |              +-------------+----------------+-------------------------+              |
                |  QueueSource  | Channel Data |             |    FilterFlow           |                | Channel Data |
                |               |    Splitter  |    Analog   |  ================       |     Analog     |              |
                |               |              |   Splitter  |  Broadcast => Filter    |     Merger     |              |
                |               |              |             |         ----------------+----------------+              |
                |               |              |             |         \=> -FullScale  |                |              |
                |               |              |             |         \=> +FullScale  |    Digital     |              |
                |               |              |             |         \=> FlatLining  |     Merger     |              |
                |               |              +-------------+-------------------------+                |              |
                |               |              |    Digitals                           |                |              |
                +---------------+--------------+-----------------------------------------------------------------------+
                */

            var indices = GetIndices(sample, configs);
            var number = indices.Count();

            var temporalOffsets = configs.Select(x => -x.TemporalOffset).Append(0);

            var temp = temporalOffsets.Select(x => x - temporalOffsets.Min()).ToList();
            var skipIndices = temp.Take(temp.Count - 1).ToList();
            var zerothIndex = temp.Last();



            var bufferSize = temp.Max() + 1;
            var skipFlowsNeeded = skipIndices.Any(x => x != 0);

            var graph = GraphDsl.Create(Source.Queue<ChannelData<float>>(10000, OverflowStrategy.Backpressure), (builder, source) =>
            {
                //Split channel data into sync data, analogs and digitals
                var channelDataSplitter = new UnzipWith<
                                    ChannelData<float>,
                                    ISyncData,
                                    IReadOnlyList<DataChannel<float>>,
                                    IReadOnlyList<DataChannel<bool>>
                                    >(cd => Tuple.Create(cd as ISyncData, cd.Analogs, cd.Digitals));
                var channelDataSplitterShape = builder.Add(channelDataSplitter);

                //Split, filter and reorder the analog channels into the required data channels
                var analogSplitter = new UnzipEnumerable<
                                        IReadOnlyList<DataChannel<float>>,
                                        DataChannel<float>
                                        >(list => indices.Select(i => list[i]).ToImmutableList(), number
                                        );
                var analogSplitterShape = builder.Add(analogSplitter);

                //Re-combine the filtered analog channels
                var analogMerger = new ZipN<DataChannel<float>>(number);
                var analogMergerShape = builder.Add(analogMerger);

                //Digital additional flows
                var additionalDigitalFlows = new List<FlowShape<DataChannel<float>, DataChannel<bool>>>();

                //Create the appropriate analog filtering flows.
                for (int i = 0; i < configs.Count(); i++)
                {
                    var skipValue = skipIndices[i];
                    //Create new flows for the analogs
                    switch (configs[i].Option)
                    {
                        // 1a) Each cfg generates one analog flow...
                        case FilterOption.PassThrough:
                            if (skipFlowsNeeded)
                            {
                                builder.From(analogSplitterShape.Out(i))
                                       .Via(
                                           builder.Add(
                                               Flow.Create<DataChannel<float>>()
                                                   .Buffer(bufferSize, OverflowStrategy.Backpressure)
                                                   .Skip(skipValue)
                                                   .Log("AnalogLog")
                                               )
                                            )
                                       .To(analogMergerShape.In(i));
                            }
                            else
                            {
                                // Pass through channels can be connected straight from the splitter to the merger.
                                builder.From(analogSplitterShape.Out(i)).To(analogMergerShape.In(i));
                            }
                            break;
                        case FilterOption.Filter:
                            // Filtered channels create a single flow and connected from the splitter to the merger.
                            var scale = configs[i].Scale;
                            var offset = configs[i].Offset;

                            var filterFlow = skipFlowsNeeded ?
                                                Flow.Create<DataChannel<float>>()
                                                    .Buffer(bufferSize, OverflowStrategy.Backpressure)
                                                    .Skip(skipValue)
                                                    .Select(x => new DataChannel<float>(x.Name, x.Value * scale + offset, x.Units))
                                                    :
                                                Flow.Create<DataChannel<float>>()
                                                    .Select(x => new DataChannel<float>(x.Name, x.Value * scale + offset, x.Units));

                            builder.From(analogSplitterShape.Out(i)).Via(builder.Add(filterFlow)).To(analogMergerShape.In(i));
                            break;
                        // 1b) OR One analog flow and 3 additional digital flows.
                        case FilterOption.CreateDigitals:
                            // Filtered channels that create digitals creates a broadcaster for the analog channel first...
                            var analogBroadcaster = new Broadcast<DataChannel<float>>(4);

                            // ...then three flows for the digitals
                            var d1Flow = builder.Add(Flow.Create<DataChannel<float>>().Select(x => new DataChannel<bool>($"{x.Name}_+FullScale", false)));
                            var d2Flow = builder.Add(Flow.Create<DataChannel<float>>().Select(x => new DataChannel<bool>($"{x.Name}_-FullScale", false)));
                            var d3Flow = builder.Add(Flow.Create<DataChannel<float>>().Select(x => new DataChannel<bool>($"{x.Name}_Flatlining", false)));

                            // ...add the digital flow shapes to be connected later
                            additionalDigitalFlows.Add(d1Flow);
                            additionalDigitalFlows.Add(d2Flow);
                            additionalDigitalFlows.Add(d3Flow);

                            // ...create the broadcaster shape
                            var analogBroadcasterShape = builder.Add(analogBroadcaster);

                            // ...create the filter flow and connect the broadcaster to the merger via the filter
                            var scaler = configs[i].Scale;
                            var offsetter = configs[i].Offset;
                            var filter = skipFlowsNeeded ?
                                            Flow.Create<DataChannel<float>>()
                                                .Buffer(bufferSize, OverflowStrategy.Backpressure)
                                                .Skip(skipValue)
                                                .Select(x => new DataChannel<float>(x.Name, x.Value * scaler + offsetter, x.Units))
                                                :
                                            Flow.Create<DataChannel<float>>()
                                                .Select(x => new DataChannel<float>(x.Name, x.Value * scaler + offsetter, x.Units));


                            // ...link the analog splitter output to the broadcaster
                            builder.From(analogSplitterShape.Out(i))
                                    .Via(filter)
                                    .To(analogBroadcasterShape);

                            builder.From(analogBroadcasterShape.Out(0)).To(analogMergerShape.In(i));
                            // ...link the broadcaster channels to the additional digital flows
                            builder.From(analogBroadcasterShape.Out(1)).Via(d1Flow);
                            builder.From(analogBroadcasterShape.Out(2)).Via(d2Flow);
                            builder.From(analogBroadcasterShape.Out(3)).Via(d3Flow);
                            break;
                        case FilterOption.NotSet:
                            throw new ArgumentException("Filter Option Not Set is not allowed.");
                    }
                }

                //Merge everything back together
                var channelDataMerger = ZipWith.Apply<
                                    ISyncData,
                                    IImmutableList<DataChannel<float>>,
                                    IReadOnlyList<DataChannel<bool>>,
                                    ChannelData<float>
                                    >(
                                        (sync, analogs, digitals) => new ChannelData<float>
                                        (
                                            analogs,
                                            digitals,
                                            sync.TimeStamp,
                                            sync.TachometerCount,
                                            sync.MasterSyncIncrement,
                                            sync.MasterSyncState,
                                            sync.SampleIndex
                                        )
                                    );
                var channelDataMergerShape = builder.Add(channelDataMerger);

                //Sink
                var sink = Sink.ActorRef<ChannelData<float>>(target, false);
                var sinkShape = builder.Add(sink);

                //_________Link stages_________

                //=====Source=====
                //Source to the channel data splitter
                if (skipFlowsNeeded)
                {
                    builder.From(source)
                           .Via(builder.Add(Flow.Create<ChannelData<float>>().Buffer(bufferSize, OverflowStrategy.Backpressure)))
                           .To(channelDataSplitterShape.In);

                    //=====Splitter=====
                    //Splitter sync data to merger.
                    builder.From(channelDataSplitterShape.Out0)
                            .Via(builder.Add(Flow.Create<ISyncData>().Buffer(bufferSize, OverflowStrategy.Backpressure).Skip(zerothIndex)))
                            .To(channelDataMergerShape.In0);

                    //Splitter analogs to analog splitter.
                    builder.From(channelDataSplitterShape.Out1)
                            .Via(builder.Add(Flow.Create<IReadOnlyList<DataChannel<float>>>().Buffer(bufferSize, OverflowStrategy.Backpressure)))
                            .To(analogSplitterShape.In);

                    //=====AdditionalDigitalFlows=====
                    if (additionalDigitalFlows.Count > 0)
                    {
                        // Additonal Digital Merger
                        var additionalDigitalMerger = new ZipWithN<DataChannel<bool>, IImmutableList<DataChannel<bool>>>(channel => channel, additionalDigitalFlows.Count);
                        var additionalDigitalMergerShape = builder.Add(additionalDigitalMerger);

                        //Combine the input digitals with the generated additional digitals
                        var digitalMerger = ZipWith.Apply<List<DataChannel<bool>>, ImmutableList<DataChannel<bool>>, IReadOnlyList<DataChannel<bool>>>((channel1, channel2) => channel1.Concat(channel2).ToList());
                        var digitalMergerShape = builder.Add(digitalMerger);

                        //Splitter digitals to digital merger.
                        builder.From(channelDataSplitterShape.Out2)
                                .Via(builder.Add(Flow.Create<IReadOnlyList<DataChannel<bool>>>().Buffer(bufferSize, OverflowStrategy.Backpressure)))
                                .To(digitalMergerShape.In0);

                        // Merge all additional flows together.
                        for (int i = 0; i < additionalDigitalFlows.Count; i++)
                        {
                            builder.From(additionalDigitalFlows[i]).To(additionalDigitalMergerShape.In(i));
                        }
                        //Additional digitals to digital merger
                        builder.From(additionalDigitalMergerShape.Out).To(digitalMergerShape.In1);

                        //=====DigitalMerger=====
                        //Digital merger to channel data merger
                        builder.From(digitalMergerShape.Out).To(channelDataMergerShape.In2);
                    }
                    else
                    {
                        // Splitter digitals to final merger.
                        builder.From(channelDataSplitterShape.Out2)
                                .Via(builder.Add(Flow.Create<IReadOnlyList<DataChannel<bool>>>().Buffer(bufferSize, OverflowStrategy.Backpressure)))
                                .To(channelDataMergerShape.In2);
                    }

                    // Analog merger to final merger.
                    builder.From(analogMergerShape.Out).To(channelDataMergerShape.In1);


                    //=====Merger=====
                    //Channel Data Merger to sink
                    builder.From(channelDataMergerShape.Out).To(sinkShape);
                }
                else
                {
                    builder.From(source).To(channelDataSplitterShape.In);

                    //=====Splitter=====
                    //Splitter sync data to merger.
                    builder.From(channelDataSplitterShape.Out0).To(channelDataMergerShape.In0);
                    //Splitter analogs to analog splitter.
                    builder.From(channelDataSplitterShape.Out1).To(analogSplitterShape.In);

                    //=====AdditionalDigitalFlows=====
                    if (additionalDigitalFlows.Count > 0)
                    {
                        // Additonal Digital Merger
                        var additionalDigitalMerger = new ZipWithN<DataChannel<bool>, IImmutableList<DataChannel<bool>>>(channel => channel, additionalDigitalFlows.Count);
                        var additionalDigitalMergerShape = builder.Add(additionalDigitalMerger);

                        //Combine the input digitals with the generated additional digitals
                        var digitalMerger = ZipWith.Apply<List<DataChannel<bool>>, ImmutableList<DataChannel<bool>>, IReadOnlyList<DataChannel<bool>>>((channel1, channel2) => channel1.Concat(channel2).ToList());
                        var digitalMergerShape = builder.Add(digitalMerger);

                        //Splitter digitals to digital merger.
                        builder.From(channelDataSplitterShape.Out2).To(digitalMergerShape.In0);

                        // Merge all additional flows together.
                        for (int i = 0; i < additionalDigitalFlows.Count; i++)
                        {
                            builder.From(additionalDigitalFlows[i]).To(additionalDigitalMergerShape.In(i));
                        }
                        //Additional digitals to digital merger
                        builder.From(additionalDigitalMergerShape.Out).To(digitalMergerShape.In1);

                        //=====DigitalMerger=====
                        //Digital merger to channel data merger
                        builder.From(digitalMergerShape.Out).To(channelDataMergerShape.In2);
                    }
                    else
                    {
                        // Splitter digitals to final merger.
                        builder.From(channelDataSplitterShape.Out2).To(channelDataMergerShape.In2);
                    }

                    // Analog merger to final merger.
                    builder.From(analogMergerShape.Out).To(channelDataMergerShape.In1);


                    //=====Merger=====
                    //Channel Data Merger to sink
                    builder.From(channelDataMergerShape.Out).To(sinkShape);
                }

                return ClosedShape.Instance;
            });

            return RunnableGraph.FromGraph(graph);
        }
    }
}