using System;
using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using Akka.Event;
using AkkaLibrary.Common.Logging;

namespace AkkaLibrary.Cluster.Actors
{
    /// <summary>
    /// Command and query control actor that is implemented as a
    /// singleton in the cluster
    /// </summary>
    public class ClusterCommandActor : ReceiveActor
    {
        private readonly ILoggingAdapter _logger;

        public ClusterCommandActor()
        {
            _logger = Context.WithIdentity("ClusterCommand");

            Default();
        }

        private void Default()
        {
            Receive<ICommand>(msg => HandleCommand(msg));
            Receive<IQuery>(msg => HandleQuery(msg));
            Receive<ICommandQueryBase>(msg => HandleUnknown(msg));
            ReceiveAny(msg => _logger.Warning("Received unknown message:{UnknownMessage}", msg));
        }

        private void HandleCommand(ICommand msg)
        {
            // Commands are forwarded on and forgotten
        }

        private void HandleQuery(IQuery msg)
        {
            // Queries expect responses
        }

        private void HandleUnknown(ICommandQueryBase msg)
        {
            _logger.Warning("{UnknownMessage} has been received and is not a Command or Query.", msg);
        }

        #region Messages

        #endregion

        #region Interfaces

        /// <summary>
        /// Base interface for all commands or queries
        /// </summary>
        public interface ICommandQueryBase
        {
            /// <summary>
            /// Unique identifier for the command/query
            /// </summary>
            /// <returns>The identity of message</returns>
            Guid Id { get; }
        }

        /// <summary>
        /// Interface that defines a command to the cluster
        /// </summary>
        public interface ICommand : ICommandQueryBase
        {

        }

        /// <summary>
        /// Interface that defines a query to the cluster
        /// </summary>
        public interface IQuery : ICommandQueryBase
        {

        }

        #endregion

        #region Factory

        /// <summary>
        /// Static factory method for creating the a singleton manager and proxy
        /// of the <see cref="ClusterCommandActor"/>
        /// </summary>
        /// <param name="system">The <see cref="ActorSystem"/>within which to create the singleton</param>
        /// <returns>A tuple containing <see cref="Props"/> for the proxy and manager in a given system</returns>
        public static (Props proxy, Props manager) CreateSingleton(ActorSystem system, string role)
        {
            var managerSettings = ClusterSingletonManagerSettings
                .Create(system)
                .WithSingletonName("cluster-command")
                .WithRemovalMargin(TimeSpan.FromSeconds(2))
                .WithHandOverRetryInterval(TimeSpan.FromMilliseconds(200));

            managerSettings = role != null ? managerSettings.WithRole(role) : managerSettings;

            var proxySettings = ClusterSingletonProxySettings
                .Create(system)
                .WithSingletonName("cluster-command-proxy")
                .WithBufferSize(1000);

            proxySettings = role != null ? proxySettings.WithRole(role) : proxySettings;
            
            var manager = ClusterSingletonManager.Props(
                Props.Create(() => new ClusterCommandActor()),
                PoisonPill.Instance,
                managerSettings
                );

            var proxy = ClusterSingletonProxy.Props(
                "/user/cluster-command",
                proxySettings
                );

            return (proxy: proxy, manager: manager);
        }

        #endregion
    }
}