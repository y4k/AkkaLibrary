using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using AkkaLibrary.Common.Logging;
using AkkaLibrary.Common.Utilities;

namespace AkkaLibrary.Cluster.Actors
{
    /// <summary>
    /// Gossip actor started on all nodes that transmits a state object to other
    /// gossip actors in a pair-wise fashion
    /// </summary>
    public class GossipActor<TState> : ReceiveActor
    {
        private readonly ILoggingAdapter _logger;
        private readonly string _name;
        private readonly IActorRef _manager;
        private readonly Func<TState, TState, TState> _stateUpdater;
        private TState _gossipState;
        private readonly Akka.Cluster.Cluster _cluster = Akka.Cluster.Cluster.Get(Context.System);
        private readonly HashSet<ActorPath> _currentNodes = new HashSet<ActorPath>();
        private HashSet<ActorPath> _actorsToTell = new HashSet<ActorPath>();
        private HashSet<ActorPath> _actorsTold = new HashSet<ActorPath>();

        public GossipActor(string name, IActorRef manager, Func<TState, TState, TState> stateUpdater)
        {
            _name = "GossipActor_" + _cluster.SelfAddress;
            _logger = Context.WithIdentity(_name);
            _manager = manager;
            _stateUpdater = stateUpdater;

            Receive<ClusterEvent.IMemberEvent>(msg =>
            {
                _logger.Debug("Member Event Received:{ClusterMemberEvent}", msg);
                msg.Match()
                    .With<ClusterEvent.MemberJoined>(HandleNewMemberEvent)
                    .With<ClusterEvent.MemberUp>(HandleNewMemberEvent)
                    .With<ClusterEvent.MemberRemoved>(HandleMemberRemoved)
                    .Default(m => _logger.Warning("Unknown Member Event Received:{ClusterMemberEvent}", m));
            });

            Receive<ClusterEvent.IClusterDomainEvent>(msg =>
            {
                _logger.Debug("Cluster Domain Event:{ClusterEvent} received.", msg);
            });

            Receive<InitiateStateUpdate>(msg =>
            {
                if (_gossipState != null)
                {
                    SendStateUpdate();
                }
            });
            Receive<NewState>(msg => 
            {
                _gossipState = msg.State;
                // Reset the hash set
                _actorsTold = new HashSet<ActorPath>();
            });
            Receive<StateUpdate>(msg => ReplyToGossip(msg));
            Receive<StateUpdateReply>(msg => HandleGossipReply(msg));
            Receive<StateRequest>(msg => Sender.Tell(new StateRequestResponse(_name, Self, _currentNodes, _gossipState)));

            // Get a snapshot of the cluster system and add all nodes to the _currentNodes set
            foreach (var item in _cluster.State.Members.Select(x => CreateFormedPath(x)))
            {
                _currentNodes.Add(item);
            }

            // Initialise gossip
            // Create the set of actors to tell from the snapshot of the system
            _actorsToTell = new HashSet<ActorPath>(_currentNodes);
            _logger.Debug("{Name} knows {Number} actors to tell. {ActorsToTell}", _name, _actorsToTell.Count, _actorsToTell.Select(x => x.Address).ToArray());
            
            // Set told actors to empty
            _actorsTold = new HashSet<ActorPath>();

            //Start gossiping at periodic intervals
            Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5),
                Self,
                new InitiateStateUpdate(),
                Self
                );
        }

        /// <summary>
        /// Receives the reply from a partner gossip actor and updates
        /// the current state accordingly
        /// </summary>
        /// <param name="msg"></param>
        private void HandleGossipReply(StateUpdateReply msg)
        {
            // Update items.
            UpdateCurrentState(msg);
        }

        /// <summary>
        /// Uses the snapshot of current nodes to start gossiping with 
        /// </summary>
        private void SendStateUpdate()
        {
            _logger.Debug("{Name} is sending new gossip message", _name);

            // If there are actors still to tell..
            if (_actorsToTell.Any())
            {
                // Randomly select an actor to tell...
                // ...and remove it from the set and add it to "told"
                _actorsToTell = _actorsToTell.RemoveAtRandom(out ActorPath actor);
                _actorsTold.Add(actor);
            }

            // Create status update
            var data = new StateUpdate(_currentNodes, _gossipState);
            // Send the message to all told actors
            foreach (var item in _actorsTold)
            {
                Context.ActorSelection(item).Tell(data);
            }
        }

        /// <summary>
        /// Upon receipt of a StateUpdate, send a reply containing internal state
        /// then update internal state with received state
        /// </summary>
        /// <param name="msg"></param>
        private void ReplyToGossip(StateUpdate msg)
        {
            _logger.Debug("{Name} is sending new gossip reply", _name);
            _logger.Debug("Data:{Data} from:{Sender} on {Self}", msg.State, Sender.Path, Self.Path);
            // Send current state
            Sender.Tell(new StateUpdateReply(msg, _currentNodes, _gossipState));
            // Update internal state
            UpdateCurrentState(msg);
        }

        /// <summary>
        /// Compare the received state and the current state and update the internal state
        /// with the union of the two.
        /// 
        /// Update the known other nodes list with those in the state message
        /// </summary>
        /// <param name="data"></param>
        private void UpdateCurrentState(IState data)
        {
            _logger.Debug("Previous state:{PreviousState} and new state:{NewState}", _gossipState, data.State);

            _gossipState = _stateUpdater(_gossipState, data.State);

            // Update the current nodes with any that are unknown
            _currentNodes.UnionWith(data.Nodes);
            _logger.Debug("{Name} knows about:{NumberNodes}", _name, _currentNodes.Count);

            // Find the actors that have not been told
            var prevCount = _actorsToTell.Count;
            _actorsToTell = _currentNodes.Except(_actorsTold).ToHashSet();
            if (prevCount != _actorsToTell.Count)
            {
                _logger.Info("{Name} had to tell {PrevCount}. Now has {CurrentCount}", _name, prevCount, _actorsToTell.Count);
            }
        }

        /// <summary>
        /// If a node is removed, remove it from the current nodes and from
        /// the actors told and to be told
        /// </summary>
        private void HandleMemberRemoved(ClusterEvent.MemberRemoved memberEvent)
        {
            var path = CreateFormedPath(memberEvent.Member);
            // Remove the node from the current nodes,
            _currentNodes.Remove(path);
            // actorsToBeTold and actorsTold
            _actorsTold.Remove(path);
            _actorsToTell.Remove(path);
        }

        private void HandleNewMemberEvent(ClusterEvent.IMemberEvent memberEvent)
        {
            var path = CreateFormedPath(memberEvent.Member);
            // Add that path to the hash set
            _currentNodes.Add(path);
            _actorsToTell.Add(path);
        }

        // Creates the path of the gossip actor for the given cluster member
        private ActorPath CreateFormedPath(Member member)
            => CreateFormedPath(member.Address);

        // Creates the path of the gossip actor for the given address
        private ActorPath CreateFormedPath(Address address)
            => ActorPath.Parse($"{address}/{ActorPath.FormatPathElements(Self.Path.Elements)}");

        #region Actor Method Overrides

        protected override void PreStart()
        {
            _cluster.Subscribe(Self, ClusterEvent.InitialStateAsEvents, new[]
            {
                typeof(ClusterEvent.IMemberEvent),
                typeof(ClusterEvent.IReachabilityEvent)
            });
        }

        protected override void PostStop()
        {
            _cluster.Unsubscribe(Self);
        }

        #endregion

        #region Messages

        public interface IState
        {
            Guid Id { get; }
            TState State { get; }
            ImmutableHashSet<ActorPath> Nodes { get; }
        }

        public sealed class NewState
        {
            public TState State { get; }
            
            public NewState(TState state)
            {
                State = state;
            }
        }

        public sealed class InitiateStateUpdate { }

        public sealed class StateUpdate : IState
        {
            public Guid Id { get; }
            public TState State { get; }

            public ImmutableHashSet<ActorPath> Nodes { get; }

            public StateUpdate(IEnumerable<ActorPath> nodes, TState state)
            {
                Nodes = nodes.ToImmutableHashSet();
                State = state;
                Id = Guid.NewGuid();
            }
        }

        public sealed class StateUpdateReply : IState
        {
            public TState State { get; }

            public Guid Id { get; }

            public ImmutableHashSet<ActorPath> Nodes { get; }

            public StateUpdateReply(StateUpdate data, IEnumerable<ActorPath> nodes, TState state) : this(data.Id, nodes, state) { }

            public StateUpdateReply(Guid id, IEnumerable<ActorPath> nodes, TState state)
            {
                Nodes = nodes.ToImmutableHashSet();
                State = state;
                Id = id;
            }
        }

        public sealed class StateRequest { }

        public sealed class StateRequestResponse
        {
            public string Name { get; }

            public IActorRef Sender { get; }

            public ImmutableHashSet<ActorPath> Nodes { get; }

            public object State { get; }

            public StateRequestResponse(string name, IActorRef sender, IEnumerable<ActorPath> nodes, object state)
            {
                Name = name;
                Nodes = nodes.ToImmutableHashSet();
                Sender = sender;
                State = state;
            }
        }

        #endregion
    }
}