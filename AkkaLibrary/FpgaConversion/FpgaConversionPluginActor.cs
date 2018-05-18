using System;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using AkkaLibrary.Common.Objects;
using Serilog;

namespace AkkaLibrary
{
    public class FpgaConversionPluginActor : ReceiveActor
    {
        private IActorRef _outputTarget;
        private IActorRef _converter;

        public FpgaConversionPluginActor(IActorRef outputTarget)
        {
            _outputTarget = outputTarget;

            _converter = Context.ActorOf(ConverterActor.GetProps(_outputTarget));

            Receive<FpgaSample>(msg => _converter.Tell(msg));
        }

        public static Props GetProps(IActorRef outputTarget) => Props.Create(() => new FpgaConversionPluginActor(outputTarget));
    }

    public class ConverterActor : ReceiveActor
    {
        private int _messagesEnqueued;
        private int _messagesDropped;

        public ConverterActor(IActorRef outputTarget)
        {
            var flowLogic = Flow.Create<FpgaSample>()
                                .SelectAsync(4, sample => Task.Run(() => ConvertSample(sample)))
                                .To(Sink.ActorRef<ChannelData<float>>(outputTarget, new FpgaConversionCompleted()));

            var source = Source.Queue<FpgaSample>(10000, OverflowStrategy.Backpressure);

            var queue = source.ToMaterialized(flowLogic, Keep.Left).Run(Context.System.Materializer());

            Receive<FpgaSample>(msg =>
            {
                queue.OfferAsync(msg).PipeTo(Self);
            });

            Receive<IQueueOfferResult>(enqueueTask =>
            {
                enqueueTask.Match()
                        .With<QueueOfferResult.Enqueued>(msg => ++_messagesEnqueued)
                        .With<QueueOfferResult.Dropped>(msg => Log.Warning("FPGA conversion actor dropped a message. Total dropped:{0}", ++_messagesDropped))
                        .With<QueueOfferResult.Failure>(msg => throw msg.Cause)
                        .With<QueueOfferResult.QueueClosed>(msg => throw new Exception("The stream queue was closed."));
            });
        }

        private ChannelData<float> ConvertSample(FpgaSample sample)
        {
            return new ChannelData<float>(
                sample.GetAnalogsAsFloats().Select(x => new DataChannel<float>(x.name, x.value)),
                sample.Bools.Select(x => new DataChannel<bool>(x.name, x.value)),
                sample.TimeStamp,
                sample.TachometerCount,
                sample.MasterSyncIncrement,
                sample.MasterSyncState,
                sample.SampleIndex
                );
        }

        public sealed class FpgaConversionCompleted { }

        public static Props GetProps(IActorRef outputTarget) => Props.Create(() => new ConverterActor(outputTarget));
    }
}