using Akka.Actor;

namespace AkkaLibrary
{
    public class ServiceDispatcher : ReceiveActor
    {
        private IActorRef _pluginStatusMonitor;
        private IActorRef _pluginDataRegistry;

        public ServiceDispatcher()
        {
        }

        private void Setup()
        {
            Receive<InitialiseNewSystem>(msg =>
            {
                //Create a new system from scratch.
                _pluginDataRegistry = Context.System.ActorOf(PluginDataRegistry.GetProps());
                _pluginStatusMonitor = Context.System.ActorOf(PluginStatusMonitor.GetProps());
            });
        }

        public static Props GetProps => Props.Create(() => new ServiceDispatcher());

        #region Messages

        public sealed class InitiateRemoteConnection { }

        public sealed class DeployActor
        {
            public Props ActorProps { get; }

            public DeployActor(Props actorProps)
            {
                ActorProps = actorProps;
            }
        }

        public sealed class InitialiseNewSystem { }

        #endregion
    }
}