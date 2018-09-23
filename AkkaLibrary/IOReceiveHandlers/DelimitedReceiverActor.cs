using System;
using System.Text;
using Akka.Actor;
using Akka.Event;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Dsl;
using AkkaLibrary.Common.Logging;
using AkkaLibrary.Common.Utilities;

namespace AkkaLibrary.IOReceiveHandlers
{
    public class DelimitedReceiverActor : ReceiveActor
    {
        private ByteString _buffer;
        private readonly ByteString _delimiter;

        public readonly ILoggingAdapter _logger;

        public DelimitedReceiverActor(string delimiter) : this(ByteString.FromString(delimiter))
        {
        }

        public DelimitedReceiverActor(byte delimiter) : this(new[] { delimiter })
        {
        }

        public DelimitedReceiverActor(byte[] delimiter) : this(ByteString.FromBytes(delimiter))
        {
        }

        public DelimitedReceiverActor(ByteString delimiter)
        {
            _buffer = ByteString.Empty;
            _delimiter = delimiter;

            _logger = Context.WithIdentity("DelimtedReceiver");

            WaitForDelimiter();
        }

        private void WaitForDelimiter()
        {
            var source = Source.ActorRef<ByteString>(1000, OverflowStrategy.Fail);

            var sink = Sink.ActorRef<string>(Self, new DelimitedStreamComplete());

            var parseLogic = Flow.Create<ByteString>()
                            .Via(Framing.Delimiter(
                                _delimiter,
                                maximumFrameLength: 2000))
                                .Select(bs => bs.ToString(Encoding.ASCII));

            var mat = source.Via(parseLogic).To(sink).Run(Context.System.Materializer());
            source.RunForeach(x => Console.WriteLine(x), Context.System.Materializer());

            Receive<ByteString>(msg =>
            {
                mat.Tell(msg);
            });

            Receive<string>(msg =>
            {
                _logger.Info(msg);
            });
        }

        public static Props GetProps(byte delimiter) => GetProps(new [] { delimiter });
        public static Props GetProps(byte[] delimiter) => GetProps(ByteString.FromBytes(delimiter));
        public static Props GetProps(string delimiter) => GetProps(ByteString.FromString(delimiter));
        public static Props GetProps(ByteString delimiter) => Props.Create(() => new DelimitedReceiverActor(delimiter));

        public sealed class DelimitedStreamComplete { }
    }

    public class DelimiterWithPayloadReceiverActor : ReceiveActor
    {
        public DelimiterWithPayloadReceiverActor(string name)
        {

        }
    }
}