using System;
using Akka.Streams;
using Akka.Streams.Stage;

namespace AkkaLibrary.Streams
{
    public class MultiSourceSync<T1, T2, TOut> : GraphStage<FanInShape<T1, T2, TOut>>
    {
        public MultiSourceSync()
        {
        }

        public Inlet<T1> In1 { get; } = new Inlet<T1>("InputOne");

        public Inlet<T2> In2 { get; } = new Inlet<T2>("InputTwo");

        public Outlet<TOut> Out { get; } = new Outlet<TOut>("Output");

        public override FanInShape<T1, T2, TOut> Shape => new FanInShape<T1, T2, TOut>(Out, In1, In2);

        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

        private sealed class Logic : InAndOutGraphStageLogic
        {
            public Logic(MultiSourceSync<T1,T2,TOut> source) : base(source.Shape)
            {

            }

            public override void OnPull()
            {
                throw new NotImplementedException();
            }

            public override void OnPush()
            {
                throw new NotImplementedException();
            }
        }
    }
}