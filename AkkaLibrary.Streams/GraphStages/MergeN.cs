using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Streams;
using Akka.Streams.Stage;
using AkkaLibrary.Common.Interfaces;
using AkkaLibrary.Common.Utilities;
using NETCoreAsio.DataStructures;

namespace AkkaLibrary.Streams.GraphStages
{
    /// <summary>
    /// Synchronises a number of input streams into a single output based on timestamp
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    public class MergeClosestN<TIn> : GraphStage<UniformFanInShape<TIn, IImmutableList<TIn>>> where TIn : class, ISyncData
    {
        private readonly int _n;

        public MergeClosestN(int n)
        {
            if(n < 2)
            {
                throw new ArgumentException("Requires at least two streams. One primary and at least one secondary.");
            }
            _n = n;
            Shape = new UniformFanInShape<TIn, IImmutableList<TIn>>(_n);
            
            PrimaryInlet = Shape.Ins[0];
            SecondaryInlets = Shape.Ins.Skip(1).ToImmutableList();
            Out = Shape.Out;
        }

        public override UniformFanInShape<TIn, IImmutableList<TIn>> Shape { get; }
        
        #region Ports

        public Outlet<IImmutableList<TIn>> Out { get; }
        public Inlet<TIn> PrimaryInlet { get; }
        public Inlet<TIn> SecondaryInlet(int n) => SecondaryInlets[n];
        public IImmutableList<Inlet<TIn>> SecondaryInlets { get; }

        #endregion

        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes)
            => new Logic(this);

        public override string ToString() => "MergeN";

        protected override Attributes InitialAttributes => Attributes.CreateName(ToString());

        #region Logic

        private sealed class Logic : OutGraphStageLogic
        {
            private readonly MergeClosestN<TIn> _source;
            private int _n;
            private Queue<ImmutableList<TIn>> _outputQueue;
            private readonly CircularQueue<TIn> _primaryQueue;
            private readonly Dictionary<Inlet<TIn>, CircularQueue<TIn>> _secondaryQueues;
            private readonly Dictionary<Inlet<TIn>, bool> _canTryMerge;
            private readonly Inlet<TIn> _primaryInlet;
            private readonly IImmutableList<Inlet<TIn>> _secondaryInlets;
            private readonly Outlet<IImmutableList<TIn>> _outlet;

            public Logic(MergeClosestN<TIn> source) : base(source.Shape)
            {
                _source = source;
                _n = _source._n;
                _primaryInlet = _source.PrimaryInlet;
                _secondaryInlets = _source.SecondaryInlets;
                _outlet = _source.Out;


                _outputQueue = new Queue<ImmutableList<TIn>>();
                _primaryQueue = new CircularQueue<TIn>(2, true);
                _secondaryQueues = _secondaryInlets
                                    .Select(x => (key: x, value: new CircularQueue<TIn>(2, true)))
                                    .ToDictionary(x => x.key, x => x.value);

                _canTryMerge = new[] { _primaryInlet }.Concat(_secondaryInlets)
                            .Select(x => (key: x, value: false))
                            .ToDictionary(x => x.key, x => x.value);

                // Set the output handler
                SetHandler(_source.Out, this);

                // Set the handler for the primary input stream
                SetHandler(
                    _primaryInlet,
                    // When an element is ready from the upstream
                    onPush: () =>
                    {
                        // Get and enqueue the element
                        var element = Grab(_primaryInlet);
                            _primaryQueue.Enqueue(element);

                        // Fill the primary queue while empty
                        if (_primaryQueue.Count < 2)
                        {
                            Pull(_primaryInlet);
                            _canTryMerge[_primaryInlet] = false;
                            return;
                        }

                        // Drop items until only 2 in the queue
                        while (_primaryQueue.Count > 2)
                        {
                            Log.Warning("{Number} items in {PrimaryQueue}. Expects 2 at most", _primaryInlet.Name, _primaryQueue.Count);
                            _primaryQueue.Dequeue();
                        }
                        _canTryMerge[_primaryInlet] = true;

                        TryMerge();
                    },
                    onUpstreamFinish: () =>
                    {
                        // Close the stage as soon as the primary stream is finished
                        CompleteStage();
                    });

                // Set the handlers for the secondary streams
                foreach (var (inlet, queue) in _secondaryQueues.Select(kvp => (kvp.Key, kvp.Value)))
                {
                    SetHandler(
                        inlet,
                        onPush: () =>
                        {
                            // Get and enqueue the element
                            var element = Grab(inlet);
                            queue.Enqueue(element);

                            // Fill the queue if there are not two items OR
                            // after dropping unsyncable items
                            if (queue.Count < 2 || DropUnsyncableSecondaryItems(_primaryQueue.Head().TimeStamp, queue))
                            {
                                // Demand an item from the inlet.
                                Pull(inlet);
                                return;
                            }

                            while (queue.Count > 2)
                            {
                                Log.Warning("{Number} items in {SecondaryQueue}. Expects 2 at most", inlet.Name, queue.Count);
                                queue.Dequeue();
                            }
                            _canTryMerge[inlet] = true;
                            
                            TryMerge();
                        },
                        onUpstreamFinish: () =>
                        {
                            // Close the stage as soon as the stream is finished
                            CompleteStage();
                        });
                }
            }

            /// <summary>
            /// When the downstream stage requests an element
            /// </summary>
            public override void OnPull()
            {
                // If there is an element that has been merged to push...
                if (_outputQueue.TryDequeue(out var element))
                {
                    // ...push out the new element
                    Push(_source.Out, element);
                }

                // Fill the primary queue if necessary
                if (_primaryQueue.Count < 2 && !HasBeenPulled(_primaryInlet))
                {
                    Pull(_primaryInlet);
                    _canTryMerge[_primaryInlet] = false;
                }
                else
                {
                    _canTryMerge[_primaryInlet] = true;
                }

                // Even if the primary required pulling, if it had at least one item in
                // it, then we can drop unsyncable items
                var canDrop = _primaryQueue.Count > 0;
                foreach (var (inlet, queue) in _secondaryQueues)
                {
                    // If there is at least 1 primary...
                    if (canDrop)
                    {
                        // ...drop secondary items too far ahead...
                        DropUnsyncableSecondaryItems(_primaryQueue.Head().TimeStamp, queue);
                    }
                    // Refill the secondary queue as necessary
                    if (queue.Count < 2 && !HasBeenPulled(inlet))
                    {
                        Pull(inlet);
                        _canTryMerge[_primaryInlet] = false;
                    }
                    else
                    {
                        _canTryMerge[_primaryInlet] = true;
                    }
                }

                TryMerge();
            }

            /// <summary>
            /// Removes items from the secondary queues that cannot possibly be
            /// synced with the primary
            /// </summary>
            /// <returns>
            /// true if any items are dropped, otherwise false
            /// </returns>
            private bool DropUnsyncableSecondaryItems(long primaryTimestamp, CircularQueue<TIn> queue)
            {
                // There can only be a single item ahead of the primary in time.
                if (queue.Count > 1)
                {
                    // If the second item in the queue is ahead of or level with the primary...
                    if (queue.Second().TimeStamp <= primaryTimestamp)
                    {
                        // ...drop the head of the secondary queue.
                        queue.Drop(1);
                        return true;
                    }
                }
                return false;
            }

            private void TryMerge()
            {
                if(_canTryMerge.Values.Any(x => x == false))
                {
                    return;
                }
                
                var ph = _primaryQueue.Head().TimeStamp;
                var pt = _primaryQueue.Second().TimeStamp;

                var canMerge = true;
                foreach (var (inlet, queue) in _secondaryQueues)
                {
                    var sh = queue.Head().TimeStamp;
                    var st = queue.Second().TimeStamp;

                    // S5 - If the secondary head is behind the primary tail...
                    if( sh >= pt )
                    {
                        // ...drop the primary head
                        _primaryQueue.Drop(1);
                        // Pull a new primary
                        if(!HasBeenPulled(_primaryInlet))
                        {
                            Pull(_primaryInlet);
                        }
                        // Once the primary has been dropped, can no longer merge
                        canMerge = false;
                    }
                    // S2 - If the secondary head and tail are before the primary...
                    else if( st <= ph )
                    {
                        // ...drop the secondary head and pull a new element
                        queue.Drop(1);
                        if(!HasBeenPulled(inlet))
                        {
                            Pull(inlet);
                        }
                        canMerge = false;
                    }
                    // S3 and 4 - If the secondary head and tail are after the primary head...
                    else if( ph < sh )
                    {
                        // S3 - If the secondary tail is before the primary tail...
                        if( st <= pt)
                        {
                            // ...sync primary head with secondary head
                        }
                        // S4 - ...otherwise...
                        else
                        {
                            // If the secondary head is closer to the primary head than the primary tail...
                            if( (sh - ph) <= (pt - sh) )
                            {
                                // ...sync with primary head and secondary head
                            }
                            // If the primary tail is closer to the secondary head than the secondary tail...
                            else if( (pt - sh) <= (st - pt) )
                            {
                                // ...sync with primary tail and secondary head...
                                // Drop the primary head...
                                _primaryQueue.Drop(1);
                                if(!HasBeenPulled(_primaryInlet))
                                {
                                    Pull(_primaryInlet);
                                }
                                // ...and stop syncing. Once the primary has been dropped, can no longer merge.
                                canMerge = false;
                            }
                            else
                            {
                                // Drop the primary head...
                                _primaryQueue.Drop(1);
                                if(!HasBeenPulled(_primaryInlet))
                                {
                                    Pull(_primaryInlet);
                                }
                                // ...and the secondary head...
                                queue.Drop(1);
                                if(!HasBeenPulled(inlet))
                                {
                                    Pull(inlet);
                                }
                                // ...and stop syncing. Once the primary has been dropped, can no longer merge.
                                canMerge = false;
                            }
                        }
                    }
                    // S1 - Secondary head is before the primary head and secondary tail
                    // is after the primary head
                    else if(sh <= ph && st > ph)
                    {
                        // If the secondary tail is after the primary tail...
                        if(st >= pt)
                        {
                            // ...sync the primary head with the secondary head
                        }
                        // If the primary head is closer to secondary head than the secondary tail...
                        else if( (ph - sh) <= (st - ph) )
                        {
                            // ...sync the primary head with the secondary head
                        }
                        // If the secondary tail is closer to primary tail than the primary head...
                        else if( (st - ph) >= ( st - pt) )
                        {
                            // Drop the secondary head but still sync...
                            queue.Drop(1);
                            // ...sync the primary head with secondary tail...
                            // ...also pull another item
                            if(!HasBeenPulled(inlet))
                            {
                                Pull(inlet);
                            }
                        }
                        else
                        {
                            canMerge = false;
                        }
                    }
                }

                if(canMerge)
                {
                    // Sync primary head with the secondary items
                    _outputQueue.Enqueue(
                        new List<TIn>
                        {
                            _primaryQueue.Dequeue()
                        }.Concat(_secondaryQueues.Values.Select(x => x.Dequeue())).ToImmutableList()
                    );

                    if(IsAvailable(_outlet))
                    {
                        Push(_outlet, _outputQueue.Dequeue());
                    }
                }
                else if(_primaryQueue.Count < 2)
                {
                    foreach (var queue in _secondaryQueues.Values)
                    {
                        DropUnsyncableSecondaryItems(_primaryQueue.Head().TimeStamp, queue);
                    }
                }

                _canTryMerge[_primaryInlet] = _primaryQueue.Count == 2;
                foreach (var key in _canTryMerge.Keys.Where(_secondaryQueues.Keys.Contains).ToList())
                {
                    _canTryMerge[key] = _secondaryQueues[key].Count == 2;
                }
            }
        }
        #endregion
    }
}