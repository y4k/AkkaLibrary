using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using AkkaLibrary.Common.Interfaces;

namespace AkkaLibrary.Cluster.Actors
{
    /// <summary>
    /// Default plugin manager class. Receives an <see cref="IPluginConfiguration"/> after
    /// which it becomes configured.
    /// 
    /// Creates a child actor of the given <see cref="Props"/> and then forwards the
    /// configuration to that actor such that it can configure itself as necessary
    /// </summary>
    public class PluginManager : ReceiveActor
    {
        private IPluginConfiguration _configuration;
        private IActorRef _server;
        private Props _childProps;
        private IActorRef _child;
        private readonly IActorRef _mediator = DistributedPubSub.Get(Context.System).Mediator;

        /// <summary>
        /// Creates an instance of the plugin manager
        /// </summary>
        public PluginManager()
        {
            Receive<IPluginConfiguration>(msg =>
            {
                _configuration = msg;
                _server = Sender;
                msg.Confirm(Sender);

                _childProps = _configuration.ActorProps;

                //Create child
                _child = Context.ActorOf(_childProps);

                foreach (var topic in _configuration.SubcribeTopics)
                {
                    _mediator.Tell(new Subscribe(topic, Self));
                }

                _child.Forward(msg);
            });

            Receive<SubscribeAck>(msg =>
            {

            });
        }
    }
}