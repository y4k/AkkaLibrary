using System;
using System.Collections.Generic;
using Akka.Streams;
using Akka.Streams.Stage;

namespace DataSynchronisation
{
    public class TimedBufferFlow<T> : GraphStage<FlowShape<T, T>>
    {
        private readonly TimeSpan _timeout;

        public TimedBufferFlow(TimeSpan timeout)
        {
            
            _timeout = timeout;
        }

        public override FlowShape<T, T> Shape => new FlowShape<T, T>(In, Out);

        public Inlet<T> In { get; } = new Inlet<T>("Input");

        public Outlet<T> Out { get; } = new Outlet<T>("Output");

        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

        /// <summary>
        /// Implementation of timed-buffer logic
        /// </summary>
        private sealed class Logic : TimerGraphStageLogic, IInHandler, IOutHandler
        {
            private Queue<T> _buffer;
            private readonly TimeSpan _timeout;
            private readonly object _timerKey;

            public Logic(TimedBufferFlow<T> source) : base(source.Shape)
            {
                _buffer = new Queue<T>(100);
                _timeout = source._timeout;
                _timerKey = new object();
            }

            public void OnPull()
            {
                throw new NotImplementedException();
            }

            public void OnPush()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Timeout has occurred. Drop items from the buffer
            /// </summary>
            /// <param name="timerKey"></param>
            protected override void OnTimer(object timerKey)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Called when the upstream stage has finished
            /// </summary>
            public void OnUpstreamFinish()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Called when the upstream stage has failed
            /// </summary>
            /// <param name="e"></param>
            public void OnUpstreamFailure(Exception e)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Called when the downstream stage has finished
            /// No more requests will come
            /// </summary>
            public void OnDownstreamFinish()
            {
                throw new NotImplementedException();
            }
        }
    }
}