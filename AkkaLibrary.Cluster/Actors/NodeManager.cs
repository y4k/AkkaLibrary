using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka;
using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Event;
using Akka.Logger.Serilog;
using AkkaLibrary.Common.Interfaces;
using AkkaLibrary.Common.Messages;
using AkkaLibrary.Common.Utilities;

namespace AkkaLibrary.Cluster.Actors
{
    public class NodeManager : ReceiveActor
    {
        private readonly ILoggingAdapter _logger;
        private IActorRef _clusterMonitor;
        private IActorRef _configurationDistributor;

        private readonly IActorRef _mediator = DistributedPubSub.Get(Context.System).Mediator;

        private Dictionary<string, HashSet<Address>> _upNodesPerRole = new Dictionary<string, HashSet<Address>>();
        private Dictionary<string, IRoleConfiguration> _roleConfigPerNode = new Dictionary<string, IRoleConfiguration>();
        private Dictionary<Address, IRoleConfiguration> _configuredNodes = new Dictionary<Address, IRoleConfiguration>();

        public NodeManager()
        {
            _logger = Context.WithIdentity(GetType().Name);

            _clusterMonitor = Context.ActorOf(Props.Create(() => new ClusterMonitor(Self)), "cluster-monitor");
            _configurationDistributor = Context.ActorOf(Props.Create(() => new ConfigurationDistributor(Self)), "config-distributor");

            Receive<NodeJoined>(msg =>
            {
            });

            Receive<NodeUp>(msg =>
            {
                /// For each role, add the up node to the hash set for that role if it doesn't already exist.
                foreach (var role in msg.NodeRoles)
                {
                    if(_upNodesPerRole.ContainsKey(role))
                    {
                        _upNodesPerRole[role].Add(msg.NodeAddress);
                    }
                    else
                    {
                        _upNodesPerRole[role] = new HashSet<Address> { msg.NodeAddress };
                    }
                }

                // Find all configurations for the roles of this new node.
                var configurations = _roleConfigPerNode.Where(kvp => msg.NodeRoles.Contains(kvp.Key)).Select(kvp => kvp.Value);

                // Configure the newly up node
                ConfigureNewUpNode(configurations, msg.NodeAddress);
            });

            Receive<NodeRemoved>(msg =>
            {
            });

            Receive<ConfigureRoles>(msg =>
            {
                foreach (var role in msg.NodeRoles)
                {
                    if(_roleConfigPerNode.ContainsKey(role))
                    {
                        _logger.Info("Replacing {OldRoleConfiguration} with {NewRoleConfiguration} for {ClusterRole}", _roleConfigPerNode[role], msg.Configuration, role);
                    }
                    _roleConfigPerNode[role] = msg.Configuration;

                    // If there are any nodes up that have this role...
                    if(_upNodesPerRole.ContainsKey(role))
                    {
                        // All nodes that need configuration sent
                        var nodes = _upNodesPerRole[role];

                        // Filter out nodes that have already been sent the same configuration
                        var nodesToConfigure = _configuredNodes.Where(kvp => nodes.Contains(kvp.Key)).Where(kvp => kvp.Value.Equals(msg.Configuration)).Select(x => x.Key);

                        ConfigureNodesWithNewConfiguration(nodesToConfigure, msg.Configuration);
                    }
                }
            });

            Receive<IConfirmation<IRoleConfiguration>>(msg =>
            {
                _logger.Info("Received confirmation of RoleConfiguration with ID:{ConfirmationId}", msg.ConfirmationId);
            });
        }

        private void ConfigureNodesWithNewConfiguration(IEnumerable<Address> nodesToConfigure, IRoleConfiguration configuration)
        {
            foreach (var node in nodesToConfigure)
            {
                ConfigureNode(node, configuration);
            }
        }

        private void ConfigureNewUpNode(IEnumerable<IRoleConfiguration> configurations, Address nodeAddress)
        {
            foreach (var cfg in configurations)
            {
                ConfigureNode(nodeAddress, cfg);
            }
        }

        private void ConfigureNode(Address nodeAddress, IRoleConfiguration configuration)
        {
            // Create the path of the configurator
            var path = $"{nodeAddress}/*/configurator";

            var actorSelection = Context.ActorSelection(path);

            // Touch the actor to check it's there
            var confirmation = actorSelection.Ask<IConfirmation<Touch>>(new Touch(), TimeSpan.FromSeconds(5));

            var result = confirmation.Result;

            actorSelection.Tell(configuration);

            _logger.Info("Attempted to configure {ClusterNode} with {RoleConfiguration} with ID:{MessageId}", nodeAddress, configuration, configuration.Id);
        }
    }

    public sealed class ConfigureRoles
    {
        public ConfigureRoles(IRoleConfiguration config, params string[] roles)
        {
            Configuration = config;
            NodeRoles = roles.ToHashSet();
        }

        public IRoleConfiguration Configuration { get; }

        public HashSet<string> NodeRoles { get; }
    }

    #region Messages

    public sealed class NodeJoined : INodeMessage
    {
        public NodeJoined(Address address, ImmutableHashSet<string> roles)
        {
            NodeAddress = address;
            NodeRoles = roles.ToArray();
        }

        public Address NodeAddress { get; }
        public string[] NodeRoles { get; }
    }

    public sealed class NodeRemoved : INodeMessage
    {
        public NodeRemoved(Address address)
        {
            NodeAddress = address;
        }

        public Address NodeAddress { get; }
    }
    
    public sealed class NodeUp : INodeMessage
    {
        public NodeUp(Address address, ImmutableHashSet<string> roles)
        {
            NodeAddress = address;
            NodeRoles = roles.ToArray();
        }

        public Address NodeAddress { get; }
        public string[] NodeRoles { get; }        
    }

    public interface INodeMessage
    {
        Address NodeAddress { get; }        
    }

    #endregion
}