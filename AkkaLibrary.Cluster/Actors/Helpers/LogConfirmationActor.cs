using System;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Event;
using AkkaLibrary.Common.Interfaces;
using AkkaLibrary.Common.Logging;
using AkkaLibrary.Common.Utilities;

namespace AkkaLibrary.Cluster.Actors
{
    public class LogConfirmationActorConfiguration : IPluginConfiguration
    {
        public LogConfirmationActorConfiguration(string name, string listenTopic)
        {
            Name = name;
            _listenTopic = listenTopic;
            ActorProps = Props.Create(() => new LogConfirmationActor(_listenTopic));
            SubcribeTopics = new string[] { _listenTopic };
        }

        public string Name { get; }

        private readonly string _listenTopic;

        public Props ActorProps { get; }

        public string[] SubcribeTopics { get; }

        public string[] PublishTopics => new string[] { };

        public Guid Id { get; } = Guid.NewGuid();

        public IConfirmation<IPluginConfiguration> GetConfirmation() => new LogConfirmationConfigurationConfirmation(Id, Name);

        IConfirmation IConfirmable.GetConfirmation() => GetConfirmation();

        private class LogConfirmationConfigurationConfirmation : IConfirmation<LogConfirmationActorConfiguration>
        {
            public LogConfirmationConfigurationConfirmation(Guid id, string pluginName)
            {
                ConfirmationId = id;
                Description = $"{pluginName} Configuration";
            }

            public Guid ConfirmationId { get; }

            public string Description { get; }
        }
    }

    internal class LogConfirmationActor : ReceiveActor
    {
        private readonly ILoggingAdapter _logger;
        private readonly IActorRef _mediator = DistributedPubSub.Get(Context.System).Mediator;
        private int _counter = 1;

        public LogConfirmationActor(string topic)
        {
            _logger = Context.WithIdentity(GetType().Name);
            _mediator.Tell(new Subscribe(topic, Self));

            Receive<SubscribeAck>(msg =>
            {
                _logger.Info("Subscription to {Topic} acknowledged from {Publisher}", msg.Subscribe.Topic, Sender);
            });

            Receive<IConfirmation>(msg =>
            {
                _logger.Info("Message confirmed:{ConfirmationId} - {Description}", msg.ConfirmationId, msg.Description);
            });

            Receive<IConfirmable>(msg =>
            {
                msg.Confirm(Sender);
                
                _logger.Info("[{Counter}] {ActorRef} says:{ReceivedMessage}. Confirmation sent.",Sender, msg, _counter);

                _counter++;
            });

            ReceiveAny(msg =>
            {
                _logger.Info("[{Counter}] {ActorRef} sent Unconfirmable message {ReceivedMessage}",_counter, Sender, msg);
                
                _counter++;
            });
        }
    }
}