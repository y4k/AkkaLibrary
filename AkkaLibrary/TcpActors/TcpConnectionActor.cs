using System;
using System.Net;
using System.Text;
using Akka.Actor;
using Akka.Event;
using Akka.IO;
using AkkaLibrary.Common.Utilities;

namespace AkkaLibrary.TcpActors
{
    public class TcpConnectionActor : ReceiveActor
    {
        public TcpConnectionActor(string name, DnsEndPoint endpoint, IActorRef supervisor)
        {
            _endpoint = endpoint;
            _supervisor = supervisor;

            _logger = Context.WithIdentity("TcpConnector");

            Ready();
            Context.System.Tcp().Tell(new Tcp.Connect(_endpoint));
        }

        private readonly DnsEndPoint _endpoint;
        private readonly IActorRef _supervisor;
        private readonly ILoggingAdapter _logger;

        private void Ready()
        {
            Receive<Tcp.Connected>(msg =>
            {
                _logger.Info($"Connected to {msg.RemoteAddress}");
                Become(Connected);
                var self = Self;

                Sender.Tell(new Tcp.Register(self, true));
            });

            Receive<Tcp.CommandFailed>(msg =>
            {
                _logger.Error($"Connection failed to open on {_endpoint}");
            });
            ReceiveAny(msg => _logger.Info(msg.ToString()));
        }

        private void Connected()
        {
            Receive<Tcp.Received>(msg =>
            {
                var received = msg.Data;
                _supervisor.Tell(new TcpDataReceived(received));
                _logger.Debug($"Message received:{msg.Data.Count}");
            });

            Receive<Tcp.PeerClosed>(msg =>
            {
                _logger.Info($"Connection closed on {_endpoint} because {msg.Cause}");
                Become(Ready);
            });

            ReceiveAny(msg =>
            {
                _logger.Info($"Received msg:{msg}");
            });
        }

        public static Props GetProps(string name, DnsEndPoint endpoint, IActorRef supervisor) => Props.Create(() => new TcpConnectionActor(name, endpoint, supervisor));

        #region Messages

        public sealed class TcpDataReceived
        {
            public ByteString Data { get; }

            public TcpDataReceived(ByteString byteString)
            {
                Data = byteString;
            }
        }

        #endregion
    }
}