using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Streams;
using Akka.Streams.Stage;
using AkkaLibrary.Common.Interfaces;

namespace AkkaLibrary.Streams.GraphStages
{
    public class MergeN<TIn> : GraphStage<UniformFanInShape<TIn, IImmutableList<TIn>>> where TIn : ISyncData
    {
        private readonly int _n;

        public MergeN(int n)
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
            private readonly MergeN<TIn> _source;
            private int _n;
            private ImmutableList<TIn> _element;
            private readonly Queue<TIn> _primaryArray;
            private readonly Queue<TIn>[] _secondaryArrays;
            private readonly Inlet<TIn> _primary;
            private readonly IImmutableList<Inlet<TIn>> _secondaries;
            private readonly Outlet<IImmutableList<TIn>> _outlet;

            public Logic(MergeN<TIn> source) : base(source.Shape)
            {
                _source = source;
                _n = _source._n;
                var upstreamToPull = new bool[_source._n];
                _primary = _source.PrimaryInlet;
                _secondaries = _source.SecondaryInlets;
                _outlet = _source.Out;

                SetHandler(_source.Out, this);

                _primaryArray = new Queue<TIn>();
                _secondaryArrays = Enumerable.Range(0,_n - 1).Select(x => new Queue<TIn>()).ToArray();

                SetHandler(
                    _primary,
                    onPush:() =>
                    {
                        var element = Grab(_primary);
                        _primaryArray.Enqueue(element);
                        while(_primaryArray.Count > 2)
                        {
                            _primaryArray.Dequeue();
                        }

                        // Fill the primary queue while less than 2
                        if(_primaryArray.Count < 2)
                        {
                            Pull(_primary);
                        }
                        // Check if merging is possible...
                        else if(CanMerge)
                        {
                            TryMerge();
                        }
                        // Otherwise fill the secondary queues
                        else
                        {
                            foreach (var inlet in _secondaries)
                            {
                                Pull(inlet);
                            }
                        }
                    },
                    onUpstreamFinish:() =>
                    {
                        // Close the stage as soon as the primary stream is finished
                        CompleteStage();
                    });

                foreach (var (inlet, queue) in _secondaries.Zip(_secondaryArrays, (x,y) => (x,y)))
                {
                    SetHandler(
                        inlet,
                        onPush:() =>
                        {
                            var element = Grab(inlet);
                            queue.Enqueue(element);
                            while(queue.Count > 2)
                            {
                                queue.Dequeue();
                            }

                            /*
                             Drop item from head of queue if seconday timestamp is less
                             than that of the primary head
                             */
                            if(queue.Peek().TimeStamp < _primaryArray.Peek().TimeStamp)
                            {
                                queue.Dequeue();
                                // Check second item as well
                                if(queue.Peek().TimeStamp < _primaryArray.Peek().TimeStamp)
                                {
                                    queue.Dequeue();
                                }
                            }

                            // If there are not two items in the queue, pull
                            if(queue.Count < 2)
                            {
                                // Demand an item from the inlet.
                                Pull(inlet);
                            }
                            else if(CanMerge)
                            {
                                TryMerge();
                            }
                        },
                        onUpstreamFinish:() =>
                        {
                            // Close the stage as soon as the stream is finished
                            CompleteStage();
                        });
                }
            }

            private bool CanMerge
                => _primaryArray.Count >= 2 && _secondaryArrays.All(x => x.Count >= 2);

            private void TryMerge()
            {
                // Try and merge items from the primary and secondary queues
                var output
                    = ImmutableList.CreateRange(
                            new[] { _primaryArray.Dequeue() }
                            .Concat(
                                _secondaryArrays.Select(x => x.Dequeue()))
                                );
                // If there is demand from the outlet
                if(IsAvailable(_outlet))
                {
                    Push(
                        _outlet,
                        output
                        );
                }
                else
                {
                    _element = output;
                }
            }

            public override void OnPull()
            {
                // Can only push an element out if it has been merged.
                if(ElementAvailable(out var element))
                {
                    // Push out the new element
                    Push(_source.Out, element);
                    _element = null;
                }
                else
                {
                    Pull(_primary);
                }
            }

            private bool ElementAvailable(out IImmutableList<TIn> element)
            {
                element = _element;
                return _element != null;
            }
        }

        #endregion
    }
}