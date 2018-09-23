using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Logger.Serilog;
using AkkaLibrary.Common.Interfaces;
using AkkaLibrary.Common.Logging;
using AkkaLibrary.Common.Utilities;

namespace AkkaLibrary.Cluster.Actors
{
    public class ConfigurationDistributor : ReceiveActor
    {
        private readonly ILoggingAdapter _logger;
        private readonly IActorRef _manager;
        private Dictionary<string, HashSet<string>> _nodeRoles = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, HashSet<string>> _roleNodes = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, IActorRef> _nodeRefs = new Dictionary<string, IActorRef>();

        public ConfigurationDistributor(IActorRef manager)
        {
            _manager = manager;
            _logger = Context.WithIdentity(GetType().Name);
            
            Receive<RegisterNode>(msg =>
            {
                var path = CreateConfiguratorPath(msg.Address);
                if (!_nodeRoles.ContainsKey(path))
                {
                    if (msg.Roles.Any(x => x == null))
                    {
                        _logger.Error("Register message for {ActorPath} has roles that are null:{ClusterRoles}", path, msg.Roles);
                        return;
                    }
                    _logger.Info("Node:{ActorPath} with roles:{ClusterRoles} has been registered.", path, msg.Roles);
                    _nodeRoles.Add(path, msg.Roles.ToHashSet());

                    foreach (var role in msg.Roles)
                    {
                        if (_roleNodes.ContainsKey(role))
                        {
                            _roleNodes[role].Add(path);
                        }
                        else
                        {
                            _roleNodes[role] = new HashSet<string> { path };
                        }
                    }
                }
                else
                {
                    _logger.Info("{ClassName} already contains a node registered with name:{ActorPath}", nameof(ConfigurationDistributor), path);
                }
            });

            Receive<UnregisterNode>(msg =>
            {
                var path = CreateConfiguratorPath(msg.Address);
                if(_nodeRoles.ContainsKey(path))
                {
                    _logger.Info("Node:{ActorPath} with roles :{ClusterRoles} has been un-registered.", path, _nodeRoles[path].ToArray());
                    _nodeRoles.Remove(path);

                    foreach (var nameSets in _roleNodes.Values)
                    {
                        if(nameSets.Contains(path))
                        {
                            nameSets.Remove(path);
                        }
                    }
                }
                else
                {
                    _logger.Info("{ClassName} does not contain a registered node with name:{ActorPath}", nameof(ConfigurationDistributor), path);
                }
            });

            Receive<NodeUp>(msg =>
            {
                var path = CreateConfiguratorPath(msg.Address);
                if(!_nodeRoles.ContainsKey(path))
                {
                    if(msg.Roles.Any(x => x == null))
                    {
                        _logger.Error("Update message for {ActorPath} has roles that are null:{ClusterRoles}", path, msg.Roles);
                        return;
                    }
                    _logger.Info("Node:{ActorPath} has been updated with roles:{ClusterRoles}.", path, msg.Roles);
                    _nodeRoles.Add(path, msg.Roles.ToHashSet());

                    foreach (var nameSets in _roleNodes.Where(x => !msg.Roles.Contains(x.Key)).Select(x => x.Value))
                    {
                        if(nameSets.Contains(path))
                        {
                            nameSets.Remove(path);
                        }
                    }
                }
                else
                {
                    _logger.Info("{ClassName} does not contain a node registered with name:{ActorPath}", nameof(ConfigurationDistributor), path);
                }
            });

            Receive<ConfigureRole>(msg =>
            {
                _logger.Error("Role was null in Configure Role Message. Roles:{ClusterRoles}", msg.Roles);

                // Get all distinct nodes that need configuring - i.e. don't configure twice unnecessarily
                var nonNullRoles = msg.Roles.Where(x => x != null).ToHashSet();

                var nodes = _nodeRoles.Where(kvp => kvp.Value.Intersect(nonNullRoles).Any()).Select(kvp => kvp.Key);

                foreach (var node in nodes)
                {
                    // Get the actor selection for the given node.
                    var selection = Context.ActorSelection(node);

                    // Check it exists by touching it. This also gives the actor ref which is preferred.
                    try
                    {
                        selection.Tell(msg.RoleConfiguration);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Could not touch Configurator actor at {ActorPath}.", node);
                        continue;
                    }
                }
            });

            Receive<IConfirmation<IRoleConfiguration>>(msg =>
            {
                if(_nodeRoles.ContainsKey(Sender.Path.ToStringWithAddress()))
                {
                    _nodeRefs[Sender.Path.ToStringWithAddress()] = Sender;
                }
            });
        }

        private string CreateConfiguratorPath(Address nodeAddress)
        {
            return $"{nodeAddress}/*/configurator";
        }

        /// <summary>
        /// Register a node such that it can be reconfigured as necessary by
        /// its defined roles
        /// </summary>
        public sealed class RegisterNode
        {
            /// <summary>
            /// Creates an instance of the <see cref="RegisterNode"/> message
            /// </summary>
            /// <param name="nodeName">The name of the node</param>
            /// <param name="nodeRoles">The roles that the node has</param>
            public RegisterNode(Address nodeName, params string[] nodeRoles)
            {
                Address = nodeName;
                Roles = nodeRoles ?? new string[] { };
            }

            /// <summary>
            /// The name of the node
            /// </summary>
            /// <returns></returns>
            public Address Address { get; }

            /// <summary>
            /// The roles of the node
            /// 
            /// Can be empty
            /// </summary>
            /// <returns></returns>
            public string[] Roles { get; }
        }

        /// <summary>
        /// Update the properties of a registered node
        /// </summary>
        public sealed class NodeUp
        {
            /// <summary>
            /// Creates an instance of the <see cref="NodeUp"/> message
            /// </summary>
            /// <param name="nodeName">The name of the node</param>
            /// <param name="nodeRoles">The roles that the node has</param>
            public NodeUp(Address nodeName, params string[] nodeRoles)
            {
                Address = nodeName;
                Roles = nodeRoles ?? new string[] { };
            }

            /// <summary>
            /// The name of the node
            /// </summary>
            /// <returns></returns>
            public Address Address { get; }

            /// <summary>
            /// The roles of the node
            /// 
            /// Can be empty
            /// </summary>
            /// <returns></returns>
            public string[] Roles { get; }
        }

        /// <summary>
        /// Removes a node from being configurable by role
        /// </summary>
        public sealed class UnregisterNode
        {
            /// <summary>
            /// Creates an instance of the <see cref="UnregisterNode"/> message
            /// </summary>
            /// <param name="nodeAddress">The name of the node</param>
            public UnregisterNode(Address nodeAddress)
            {
                Address = nodeAddress;
            }

            /// <summary>
            /// The name of the node
            /// </summary>
            /// <returns></returns>
            public Address Address { get; }
        }

        /// <summary>
        /// For a given number of roles, apply a chosen configuration
        /// </summary>
        public sealed class ConfigureRole
        {
            /// <summary>
            /// Creates an instance of the <see cref="ConfigureRole"/>
            /// with a single role
            /// </summary>
            /// <param name="role"></param>
            /// <param name="config">The role configuration to apply</param>
            public ConfigureRole(string role, IRoleConfiguration config) : this(new[]{role}, config) { }

            /// <summary>
            /// Creates an instance of the <see cref="ConfigureRole"/>
            /// with multiple roles
            /// </summary>
            /// <param name="roles">Array of roles</param>
            /// <param name="config">The role configuration to apply</param>
            public ConfigureRole(string[] roles, IRoleConfiguration config)
            {
                Roles = roles.Distinct().ToArray() ?? new string[] { };
                RoleConfiguration = config;
            }

            /// <summary>
            /// The roles to configure or re-configure
            /// </summary>
            /// <returns></returns>
            public string[] Roles { get; }

            public IRoleConfiguration RoleConfiguration { get; }
        }
    }
}