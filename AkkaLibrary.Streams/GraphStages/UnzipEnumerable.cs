using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Akka.Streams;
using Akka.Streams.Stage;
using Akka.Util.Internal;

namespace AkkaLibrary.Streams.GraphStages
{
    /// <summary>
    /// Fan out graph stage that splits a stream of enumerables, each of length n,
    /// into n streams.
    /// </summary>
    public class UnzipEnumerable<TIn, TOut> : GraphStage<UniformFanOutShape<TIn, TOut>>
    {
        #region Logic 

        /// <summary>
        /// Logic for the <see cref="UnzipEnumerable{TIn, TOut}"/> graph stage
        /// </summary>
        private sealed class Logic : InGraphStageLogic
        {
            private readonly UnzipEnumerable<TIn, TOut> _stage;
            private int _pendingCount;
            private Dictionary<string, bool> _pendingFlags;
            private int _downstreamRunning;
            private readonly Func<TIn, IImmutableList<TOut>> _unzipper;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="stage"><see cref="UnzipEnumerable{TIn, TOut}"/> stage</param>
            /// <returns></returns>
            public Logic(UnzipEnumerable<TIn, TOut> stage) : base(stage.Shape)
            {
                _stage = stage;
                _pendingCount = _stage._n;
                _downstreamRunning = _stage._n;

                _unzipper = _stage._unzipper;

                _pendingFlags = new Dictionary<string, bool>();

                foreach (var outlet in _stage.Outlets)
                {
                    _pendingFlags.Add(outlet.Name, true);
                }

                SetHandler(_stage.In, this);

                _stage.Outlets.ForEach(i =>
                {
                    SetHandler(i, onPull: () =>
                    {
                        --_pendingCount;
                        _pendingFlags[i.Name] = false;

                        if (_pendingCount == 0)
                        {
                            Pull(_stage.In);
                        }
                    },
                    onDownstreamFinish: () =>
                    {
                        _downstreamRunning--;
                        if (_downstreamRunning == 0)
                        {
                            CompleteStage();
                        }
                        else
                        {
                            if (_pendingFlags[i.Name])
                            {
                                _pendingCount--;
                            }
                            if (_pendingCount == 0 && !HasBeenPulled(_stage.In))
                            {
                                Pull(_stage.In);
                            }
                        }
                    });
                });
            }

            public override string ToString() => "UnzipEnumerable";

            public override void OnPush()
            {
                var elements = _stage._unzipper(Grab(_stage.In));

                var index = 0;
                foreach (var outlet in _stage.Outlets)
                {
                    if(!IsClosed(outlet))
                    {
                        Push(outlet, elements[index++]);
                        _pendingFlags[outlet.Name] = true;
                    }
                }

                _pendingCount = _downstreamRunning;
            }
        }

        #endregion

        private readonly Func<TIn, IImmutableList<TOut>> _unzipper;
        private readonly int _n;

        public UnzipEnumerable(Func<TIn, IImmutableList<TOut>> unzipper, int n)
        {
            _unzipper = unzipper;
            _n = n;

            Shape = new UniformFanOutShape<TIn, TOut>(n);

            Outlets = Shape.Outs;
            In = Shape.In;
        }

        public IImmutableList<Outlet<TOut>> Outlets { get; }

        public Outlet<TOut> Out(int i) => Outlets[i];

        public Inlet<TIn> In { get; }

        public override string ToString() => "UnzipEnumerable";

        public override UniformFanOutShape<TIn, TOut> Shape { get; }

        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

        protected override Attributes InitialAttributes => Attributes.CreateName("UnzipEnumerable");
    }
}