using System.Collections.Generic;
using System.Collections.Immutable;
using Akka.Streams;
using Akka.Streams.Stage;
using Akka.Util.Internal;

namespace AkkaLibrary.Streams.Graphs
{
    public class RoundRobinFanOut<T> : GraphStage<UniformFanOutShape<T, T>>
    {
        #region Logic

        private sealed class Logic : InGraphStageLogic
        {
            private readonly RoundRobinFanOut<T> _stage;
            private int _downstreamRunning;
            private readonly IEnumerator<Outlet<T>> _outletEnumerator;

            private Outlet<T> _currentOutlet => _outletEnumerator.Current;

            private Outlet<T> _nextOutlet
            {
                get
                {
                    if(!_outletEnumerator.MoveNext())
                    {
                        _outletEnumerator.Reset();
                        _outletEnumerator.MoveNext();
                    }

                    return _currentOutlet;
                }
            }

            public Logic(RoundRobinFanOut<T> stage) : base(stage.Shape)
            {
                // Copy of the stage;
                _stage = stage;

                // Count of downstream running stages
                _downstreamRunning = _stage._n;

                // Enumerator over all the outlets
                _outletEnumerator = _stage.Outlets.GetEnumerator();

                // Set the current outlet by moving next once.
                _outletEnumerator.MoveNext();

                // Inlet handler
                SetHandler(_stage.In, this);

                foreach (var outlet in _stage.Outlets)
                {
                    SetHandler(outlet,
                    onPull: () =>
                    {
                        if(outlet == _currentOutlet)
                        {
                            Pull(_stage.In);
                        }
                    },
                    onDownstreamFinish:() =>
                    {
                        --_downstreamRunning;
                        if(_downstreamRunning == 0)
                        {
                            CompleteStage();
                        }
                        else
                        {
                            if(!HasBeenPulled(_stage.In))
                            {
                                Pull(_stage.In);
                            }
                        }
                    });
                }
            }

            public override void OnPush()
            {
                // Get the input element
                var element = Grab(_stage.In);

                // Push the element through the current outlet.
                if(!IsClosed(_currentOutlet))
                {
                    Push(_currentOutlet, element);
                }
                // Set the outlet to the next and check whether it is available to be pushed
                // If so, pull.
                if((IsAvailable(_nextOutlet)))
                {
                    Pull(_stage.In);
                }
            }

            public override string ToString() => _stage.ToString();
        }

        #endregion

        public RoundRobinFanOut(int n)
        {
            _n = n;
            
            Shape = new UniformFanOutShape<T, T>(_n);

            In = Shape.In;
            Outlets = Shape.Outs;
        }

        private readonly int _n;
        
        public Inlet<T> In { get; }

        public Outlet<T> Out(int i) => Outlets[i];

        public IImmutableList<Outlet<T>> Outlets { get; }

        public override string ToString() => "RoundRobinFanOut";

        public override UniformFanOutShape<T, T> Shape { get; }

        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

        protected override Attributes InitialAttributes => Attributes.CreateName(ToString());
    }
}