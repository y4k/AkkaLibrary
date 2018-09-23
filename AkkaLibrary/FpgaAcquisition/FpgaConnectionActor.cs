using System;
using System.Net;
using Akka.Actor;
using Akka.IO;
using Serilog;

namespace AkkaLibrary
{
    public class FpgaConnectionActor : ReceiveActor
    {
        private readonly EndPoint _endPoint;
        private IActorRef _outputTarget;
        private TimeSpan _retryConnectionTimeout;

        public FpgaConnectionActor(EndPoint endPoint, TimeSpan retryConnectionTimeout, IActorRef outputTarget)
        {
            _endPoint = endPoint;
            
            _outputTarget = outputTarget;
            _retryConnectionTimeout = retryConnectionTimeout;

            Ready();

            Context.System.Tcp().Tell(new Tcp.Connect(_endPoint));
        }

        public static Props GetProps(EndPoint endPoint, TimeSpan timeout, IActorRef outputTarget) => Props.Create(() => new FpgaConnectionActor(endPoint, timeout, outputTarget));

        private void Ready()
        {
            Receive<Tcp.Connected>(msg =>
            {
                Log.Information($"Connected to {msg.RemoteAddress} from {msg.LocalAddress}. Registering.");
                Sender.Tell(new Tcp.Register(Self, true));
            });

            Receive<Tcp.CommandFailed>(msg =>
            {
                Log.Error($"Connection failed to open on {_endPoint} with {msg.Cmd}");
                Context.System.Scheduler.ScheduleTellOnce(_retryConnectionTimeout, Context.System.Tcp(), new Tcp.Connect(_endPoint), Self);
            });

            Receive<Tcp.Received>(msg =>
            {
                var received = msg.Data;
                _outputTarget.Tell(received);
                Log.Debug($"TCP Message received:{msg.Data}");
            });

            Receive<Tcp.PeerClosed>(msg =>
            {
                Log.Information($"Remote connection closed on {_endPoint}. Retrying connection.");
                Context.System.Scheduler.ScheduleTellOnce(_retryConnectionTimeout, Context.System.Tcp(), new Tcp.Connect(_endPoint), Self);
            });

            ReceiveAny(msg =>
            {
                Log.Information($"Received msg:{msg}");
            });
        }
    }
}
