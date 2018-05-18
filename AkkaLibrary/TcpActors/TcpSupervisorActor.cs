using System.Collections.Generic;
using System.Net;
using Akka.Actor;
using Akka.Event;
using Akka.IO;
using Akka.Streams.Dsl;
using AkkaLibrary.Common.Utilities;

namespace AkkaLibrary.TcpActors
{
    public class TcpSupervisorActor : ReceiveActor
    {
        private readonly string _name = Context.Self.Path.Name;

        public TcpSupervisorActor(string name)
        {
            _logger = Context.WithIdentity("TcpSupervisor");
            Ready();
        }

        public static Props GetProps(string name) => Props.Create(() => new TcpSupervisorActor(name));

        private void Ready()
        {
            Receive<NewConnection>(msg =>
            {
                _endpoint = msg.EndPoint;
                var name = $"tcp-connection-{_name}";
                var self = Self;

                _connectionActor = Context.ActorOf(TcpConnectionActor.GetProps(name, _endpoint, self));

                Become(Working);
            });
            
            //Add subscription logic receive handlers
            SubscriptionHandlers();
        }

        private void Working()
        {
            Receive<NewConnection>(msg =>
            {
                Context.Stop(_connectionActor);

                _endpoint = msg.EndPoint;
                var name = $"tcp-connection-{_name}";
                var self = Self;

                _connectionActor = Context.ActorOf(TcpConnectionActor.GetProps(name, _endpoint, self));
                
                Become(Working);
            });

            Receive<CloseConnection>(msg =>
            {
                Context.Stop(_connectionActor);
                Become(Ready);
            });

            Receive<TcpConnectionActor.TcpDataReceived>(msg =>
            {
                foreach (var sub in _subscribers)
                {
                    sub.Value.Tell(msg.Data);
                }
            });

            //Add subscription logic receive handlers
            SubscriptionHandlers();
        }

        private void SubscriptionHandlers()
        {
            Receive<Subscribe>(msg =>
            {
                if(_subscribers.ContainsKey(msg.Name))
                {
                    _logger.Info($"Subscriber {msg.Name} is already subscribed.");
                }
                else
                {
                    _subscribers[msg.Name] = msg.Ref;
                }
            });

            Receive<Unsubscribe>(msg =>
            {
                if(_subscribers.ContainsKey(msg.Name))
                {
                    _subscribers.Remove(msg.Name);
                }
            });
        }

        private DnsEndPoint _endpoint;
        private IActorRef _connectionActor;
        private Dictionary<string, IActorRef> _subscribers = new Dictionary<string, IActorRef>();
        private readonly ILoggingAdapter _logger;


        #region Messages

        public sealed class NewConnection
        {
            public DnsEndPoint EndPoint { get; }

            public NewConnection(DnsEndPoint endpoint)
            {
                EndPoint = endpoint;
            }
        }

        public sealed class CloseConnection { }

        public sealed class Subscribe
        {
            public string Name { get; }
            public IActorRef Ref { get; }

            public Subscribe(string name, IActorRef actor)
            {
                Name = name;
                Ref = actor;
            }
        }

        public sealed class Unsubscribe
        {
            public string Name { get; }

            public Unsubscribe(string name)
            {
                Name = name;
            }
        }

        #endregion
    }
}