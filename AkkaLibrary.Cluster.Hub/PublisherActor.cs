using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Event;

namespace AkkaLibrary.Cluster.Hub
{
    /// <summary>
    /// Simple publisher actor that takes any message it receives and
    /// publishes it to the given topic
    /// </summary>
    public class PublisherActor : ReceiveActor
    {
        // PubSub mediator
        private readonly IActorRef _mediator = DistributedPubSub.Get(Context.System).Mediator;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="topic">Publish topic</param>
        public PublisherActor(string topic)
        {
            // Receive all messages and publish to topic
            ReceiveAny(msg => 
            {
                _mediator.Tell(new Publish(topic, msg, sendOneMessageToEachGroup:true));
            });
        }
    }
}