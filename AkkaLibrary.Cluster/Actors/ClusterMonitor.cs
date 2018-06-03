using System;
using System.Linq;
using Akka;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using Akka.Logger.Serilog;
using AkkaLibrary.Common.Logging;
using AkkaLibrary.Common.Utilities;

namespace AkkaLibrary.Cluster.Actors
{
    /// <summary>
    /// Simple cluster event monitor
    /// </summary>
    public class ClusterMonitor : ReceiveActor
    {
        private readonly ILoggingAdapter _logger;
        private readonly Akka.Cluster.Cluster _cluster = Akka.Cluster.Cluster.Get(Context.System);
        private readonly IActorRef _manager;

        /// <summary>
        /// Creates an instance of the <see cref="ClusterMonitor"/>
        /// </summary>
        public ClusterMonitor(IActorRef manager)
        {
            _manager = manager;
            _logger = Context.WithIdentity(GetType().Name);

            Receive<ClusterEvent.IMemberEvent>(msg =>
            {
                msg.Match()
                    .With<ClusterEvent.MemberJoined>(HandleMemberJoined)
                    .With<ClusterEvent.MemberUp>(HandleMemberUp)
                    .With<ClusterEvent.MemberRemoved>(HandleMemberRemoved)
                    .Default(m => _logger.Warning("Unknown Member Event Received:{ClusterEvent}", msg));
            });

            Receive<ClusterEvent.IClusterDomainEvent>(msg =>
            {
                _logger.Info("Cluster Domain Event:{ClusterEvent} received.", msg);
            });
        }

        private void HandleMemberRemoved(ClusterEvent.MemberRemoved msg)
        {
            _logger.Info("Member Removed:{ClusterMember}", msg.Member);
            var address = msg.Member.Address;

            _manager.Tell(new NodeRemoved(address));
        }

        private void HandleMemberUp(ClusterEvent.MemberUp msg)
        {
            _logger.Info("Member:{ClusterMember} now {ClusterMemberState}", msg.Member, msg.Member.Status);
            var address = msg.Member.Address;
            var roles = msg.Member.Roles;

            _manager.Tell(new NodeUp(address, roles));
        }

        private void HandleMemberJoined(ClusterEvent.MemberJoined msg)
        {
            _logger.Info("Member Joined:{ClusterMember} at {NodeAddress}", msg.Member, msg.Member.Address);
            var address = msg.Member.Address;
            var roles = msg.Member.Roles;

            _manager.Tell(new NodeJoined(address, roles));
        }

        protected override void PreStart()
        {
            _cluster.Subscribe(Self, ClusterEvent.InitialStateAsEvents, new[]
            {
                typeof(ClusterEvent.IMemberEvent),
                typeof(ClusterEvent.IClusterDomainEvent)
            });
        }

        protected override void PostStop()
        {
            _cluster.Unsubscribe(Self);
        }
    }
}