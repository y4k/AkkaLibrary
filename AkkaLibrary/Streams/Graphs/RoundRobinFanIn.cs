using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Akka.Streams;
using Akka.Streams.Stage;

namespace AkkaLibrary.Streams.Graphs
{
    public class RoundRobinFanIn<T> : GraphStage<UniformFanInShape<T, T>>
    {
        #region Logic

        private sealed class Logic : OutGraphStageLogic
        {
            private readonly RoundRobinFanIn<T> _stage;
            private readonly IEnumerator<Inlet<T>> _inletEnumerator;
            private int _upstreamRunning;
            private Inlet<T> _currentInlet => _inletEnumerator.Current;
            private Inlet<T> NextInlet()
            {
                    if(!_inletEnumerator.MoveNext())
                    {
                        _inletEnumerator.Reset();
                        _inletEnumerator.MoveNext();
                    }

                    return _currentInlet;
            }

            public Logic(RoundRobinFanIn<T> stage) : base(stage.Shape)
            {
                // Copy of the stage
                _stage = stage;

                // Count of upstream running stages
                _upstreamRunning = _stage._n;

                // Enumerator over all the inlets
                _inletEnumerator = _stage.Inlets.GetEnumerator();

                // Set the current inlet by moving next once
                _inletEnumerator.MoveNext();

                // Outlet handler
                SetHandler(_stage.Out, this);

                foreach (var inlet in _stage.Inlets)
                {
                    SetHandler(inlet,
                    onPush:() =>
                    {
                        // Inlet has a new element.

                        // If the inlet is the current inlet and the outlet is
                        // available to be pushed, grab the element and push it.
                        if(inlet == _currentInlet && IsAvailable(_stage.Out))
                        {
                            var element = Grab(inlet);
                            Push(_stage.Out, element);

                            // move the iterator to the next
                            NextInlet();
                        }
                    },
                    onUpstreamFinish:() =>
                    {
                        --_upstreamRunning;
                        if(_upstreamRunning == 0)
                        {
                            CompleteStage();
                        }
                        else
                        {
                            if (inlet == _currentInlet)
                            {
                                NextInlet();
                            }
                        }
                    });
                }
            }

            // Request from the outlet that an item can be transmitted.
            public override void OnPull()
            {
                // If there are no inlets open then complete the stage
                if(_upstreamRunning == 0)
                {
                    CompleteStage();
                }
                
                // If the current inlet is closed then move to the next
                while((IsClosed(_currentInlet)))
                {
                    NextInlet();
                }

                // If the current inlet has an element to push then grab it
                
                // If there is an element to grab then do so.
                if(IsAvailable(_currentInlet))
                {
                    var element = Grab(_currentInlet);

                    // then push it to the outlet.
                    Push(_stage.Out, element);

                    NextInlet();
                }
                // Otherwise, pull on the current inlet if it has not been done already
                else if(!HasBeenPulled(_currentInlet))
                {
                    // Create demand on the inlet
                    Pull(_currentInlet);
                }
            }
        }

        #endregion

        public RoundRobinFanIn(int n)
        {
            _n = n;
            Shape = new UniformFanInShape<T, T>(_n);

            Inlets = Shape.Ins;
            Out = Shape.Out;
        }

        public override UniformFanInShape<T, T> Shape { get; }

        private readonly int _n;

        public Inlet<T> In(int i) => Inlets[i];

        public Outlet<T> Out { get; }

        public IImmutableList<Inlet<T>> Inlets { get; }

        public override string ToString() => "RoundRobinFanIn";

        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

        protected override Attributes InitialAttributes => Attributes.CreateName(ToString());
    }
}