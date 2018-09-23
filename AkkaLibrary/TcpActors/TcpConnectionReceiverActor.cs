using System;
using System.Net;
using Akka.Actor;
using Akka.IO;

namespace AkkaLibrary.TcpActors
{
    public class TcpConnectionReceiverActor : ReceiveActor
    {
        public TcpConnectionReceiverActor(string name)
        {
        }

        public static Props GetProps(string name) => Props.Create(() => new TcpConnectionReceiverActor(name));
    }

    public class EchoServer : ReceiveActor
    {
        private readonly int _port;

        public EchoServer(string name, int port)
        {
            _port = port;
            Ready();
        }

        public static Props GetProps(string name, int port) => Props.Create(() => new EchoServer(name, port));

        protected void Ready()
        {
            Receive<Tcp.Bound>(msg =>
            {
                Console.WriteLine("Listening on {0}", msg.LocalAddress);
            });

            Receive<Tcp.Connected>(msg =>
            {
                var connection = Context.ActorOf(Props.Create(() => new EchoConnection("echo-connection", Sender)));
                Sender.Tell(new Tcp.Register(connection));
            });

            Context.System.Tcp().Tell(new Tcp.Bind(Self, new IPEndPoint(IPAddress.Any, _port)));
        }
    }

    public class EchoConnection : ReceiveActor
    {
        private readonly IActorRef _connection;

        public EchoConnection(string name, IActorRef connection)
        {
            _connection = connection;
            Ready();
        }

        protected void Ready()
        {
            Receive<Tcp.Received>(msg =>
            {
                if (msg.Data[0] == 'x')
                    Context.Stop(Self);
                else
                    _connection.Tell(Tcp.Write.Create(msg.Data));
            });
        }
    }
}