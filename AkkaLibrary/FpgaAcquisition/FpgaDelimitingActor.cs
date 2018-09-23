using System;
using Akka;
using Akka.Actor;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Dsl;
using Serilog;

namespace AkkaLibrary
{
    public class FpgaDelimitingActor : ReceiveActor
    {
        public FpgaDelimitingActor(ByteString delimiter, int maxFrameLength, IActorRef sampleAssembler)
        {
            _maxFrameLength = maxFrameLength;
            _delimiter = delimiter;
            _sampleAssembler = sampleAssembler;

            _framingLogic = CreateFramingLogic(_delimiter, _maxFrameLength, _sampleAssembler);

            Working();
        }

        private ISourceQueueWithComplete<ByteString> CreateFramingLogic(ByteString delimiter, int maxFrameLength, IActorRef sampleAssembler)
        {
            var delimitLogic = Flow.Create<ByteString>()
                                .Via(Framing.Delimiter(delimiter, maxFrameLength, false))
                                .Where(bs => bs.Count > 0)
                                .To(Sink.ActorRef<ByteString>(sampleAssembler, new FpgaPluginMessages.StreamComplete()));

            var source = Source.Queue<ByteString>(10000, OverflowStrategy.Backpressure);

            return source.ToMaterialized(delimitLogic, Keep.Left).Run(Context.System.Materializer());
        }

        private void Working()
        {
            Receive<ByteString>(msg =>
            {
                //Attempts to enqueue the msg and pipes the task result back to self to check outcome
                _framingLogic.OfferAsync(msg).PipeTo(Self);
            });

            Receive<IQueueOfferResult>(enqueueTask =>
            {
                enqueueTask.Match()
                        .With<QueueOfferResult.Enqueued>(msg => ++_messagesEnqueued)
                        .With<QueueOfferResult.Dropped>(msg => Log.Warning("FPGA framing actor dropped a message. Total dropped:{0}", ++_messagesDropped))
                        .With<QueueOfferResult.Failure>(msg => throw msg.Cause)
                        .With<QueueOfferResult.QueueClosed>(msg => throw new Exception("The stream queue was closed."));
            });
        }
        
        public static Props GetProps(ByteString delimiter, int maxFrameLength, IActorRef sampleAssembler)
                                => Props.Create(() => new FpgaDelimitingActor(delimiter, maxFrameLength, sampleAssembler));

        private IActorRef _sampleAssembler;
        private ByteString _delimiter;
        private int _maxFrameLength;
        private ISourceQueueWithComplete<ByteString> _framingLogic;
        private int _messagesEnqueued;
        private int _messagesDropped;
    }
}
