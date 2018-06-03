using System;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using AkkaLibrary.Common.Interfaces;
using AkkaLibrary.Cluster.Configuration;
using AkkaLibrary.Cluster.Interfaces;
using AkkaLibrary.Common.Logging;

namespace AkkaLibrary.Cluster.Actors
{
    /// <summary>
    /// Root actor for a node in a cluster
    /// </summary>
    public class NodeConfigurator : ReceiveActor
    {
        private IPropsFactory _actorPropsFactory;
        private readonly ILoggingAdapter _logger;
        private readonly Config _systemConfiguration;

        /// <summary>
        /// Creates an instance of <see cref="NodeConfigurator"/>
        /// </summary>
        public NodeConfigurator()
        {
            _logger = Context.WithIdentity(GetType().Name);

            _systemConfiguration = Context.System.Settings.Config;

            var actorFactoryTypeName = _systemConfiguration.GetString(ClusterNodeConstants.HoconFactoryNode);

            var factoryType = Type.GetType(actorFactoryTypeName);

            _actorPropsFactory = Activator.CreateInstance(factoryType) as IPropsFactory;

            DefaultBehaviour();
        }

        /// <summary>
        /// Default behaviour of the configurator
        /// 
        /// Receives an <see cref="IRoleConfiguration"/> and creates children from the
        /// <see cref="IPluginConfiguration"/> list supplied
        /// </summary>
        private void DefaultBehaviour()
        {
            Receive<IRoleConfiguration>(msg =>
            {
                msg.Confirm(Sender);
                if (msg.Configs.Select(x => x.Name).Distinct().Count() != msg.Configs.Length)
                {
                    var ex = new ArgumentException("Configuration names are not distinct.");
                    _logger.Error(ex, "Configuration names are not distinct:{ConfigurationNames}", msg.Configs.Select(x => x.Name).ToArray());
                    throw ex;
                }

                foreach (var cfg in msg.Configs)
                {
                    var actor = CreateChildIfNotExist(cfg);
                    actor.Tell(cfg, Sender);
                }
            });

            Receive<INodeCommand>(msg =>
            {
                msg.Confirm(Sender);
                HandleCommand(msg);
            });

            Receive<IPluginCommand>(msg => 
            {
                msg.Confirm(Sender);
                HandlePluginCommand(msg);
            });

            Receive<IConfirmable>(msg => msg.Confirm(Sender));
        }

        /// <summary>
        /// Node specific commands that are handled here by the <see cref="NodeConfigurator"/>
        /// </summary>
        /// <param name="msg"></param>
        private void HandleCommand(INodeCommand msg)
        {
            switch (msg)
            {
                default:
                    _logger.Warning("Received unknown command:{MessageType} - ID:{MessageId}", msg.GetType(), msg.Id);
                    break;
            }
        }

        /// <summary>
        /// Handles plugin commands by checking if an actor of the specified name exists and then
        /// forwarding the message on if it does so
        /// </summary>
        /// <param name="msg"></param>
        private void HandlePluginCommand(IPluginCommand msg)
        {
            var child = Context.Child(msg.PluginName);
            if(child.IsNobody())
            {
                child.Forward(msg);
            }
        }

        /// <summary>
        /// Creates a child actor from a given <see cref="IPluginConfiguration"/> if
        /// one does not exist already.
        /// </summary>
        /// <param name="config"><see cref="IPluginConfiguration"/></param>
        /// <returns>The <see cref="IActorRef"/> of the child actor</returns>
        private IActorRef CreateChildIfNotExist(IPluginConfiguration config)
        {
            var child = Context.Child(config.Name);

            if(child.IsNobody())
            {
                return Context.ActorOf(_actorPropsFactory.CreateProps(), config.Name);
            }
            return child;
        }
    }
}