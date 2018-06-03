using System;
using Akka;
using Akka.Logger.Serilog;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Stage;

namespace DataSynchronisation
{
    /// <summary>
    /// A simple stream decimator that releases elements downstream once they exceeded
    /// a set distance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SpatialDecimator<T> : GraphStage<FlowShape<T, T>> where T : ITimedObject
    {
        private readonly double _spacingMetres;
        private readonly double _tachosPerMetre;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tachosPerMetre">Tachos per metre</param>
        /// <param name="spacingMetres">Minimum spacing between samples in metres</param>
        public SpatialDecimator(double tachosPerMetre, double spacingMetres)
        {
            _spacingMetres = spacingMetres;
            _tachosPerMetre = tachosPerMetre;
        }

        /// <summary>
        /// Shape of the graph stage
        /// Defines the number and type of inlets and outlets
        /// </summary>
        /// <typeparam name="T">Input Type</typeparam>
        /// <typeparam name="T">Output Type</typeparam>
        /// <returns></returns>
        public override FlowShape<T, T> Shape => new FlowShape<T, T>(In, Out);

        /// <summary>
        /// The inlet of the graph stage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Inlet<T> In { get; } = new Inlet<T>("SpatialDecimator.Input");

        /// <summary>
        /// The outlet of the graph stage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Outlet<T> Out { get; } = new Outlet<T>("SpatialDecimator.Output");

        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

        /// <summary>
        /// Implementation of stream stage logic
        /// </summary>
        private sealed class Logic : InAndOutGraphStageLogic
        {
            private readonly long _requiredTachos;
            private readonly SpatialDecimator<T> _source;
            private long? _lastTachoCount;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="source"></param>
            /// <returns></returns>
            public Logic(SpatialDecimator<T> source) : base(source.Shape)
            {
                _source = source;
                _requiredTachos = (long) Math.Max(_source._spacingMetres * _source._tachosPerMetre, 1);
                SetHandler<T>(source.In, onPush: OnPush);
                SetHandler<T>(source.Out, onPull: OnPull);
            }

            public override void PreStart()
            {
                Serilog.Log.ForContext("Identity", "SpatialDecimator")
                .Debug("Created with separation {SpatialSeparation} metre(s) and tachos per metre {TachosPerMetre}", _source._spacingMetres, _source._tachosPerMetre);
            }

            /// <summary>
            /// Called when the downstream stage requests an element
            /// </summary>
            public override void OnPull() => Pull(_source.In);

            /// <summary>
            /// Called when an upstream stage has an element ready
            /// </summary>
            public override void OnPush()
            {
                var element = Grab(_source.In);

                if(_lastTachoCount == null || ShouldReleaseElement(element))
                {
                    // If the element is eligible to be pushed downstream...
                    _lastTachoCount = element.TachometerCount;
                    Push(_source.Out, element);
                }
                else
                {
                    if(element.TachometerCount < _lastTachoCount)
                    {
                        Log.ForContext("Identity", "SpatialDecimator")
                        .Warning("Tacho count of element {ElementTachoCount} is less than that of previously released {PreviousTachoCount}", element.TachometerCount, _lastTachoCount);
                    }
                    // ...otherwise, request a new element
                    Pull(_source.In);
                }
            }

            /// <summary>
            /// Determines if the given element should be released downstream
            /// according to its tacho
            /// </summary>
            /// <param name="element"></param>
            /// <returns></returns>
            private bool ShouldReleaseElement(T element)
            {
                return element.TachometerCount - _lastTachoCount >= _requiredTachos;
            }
        }
    }
}