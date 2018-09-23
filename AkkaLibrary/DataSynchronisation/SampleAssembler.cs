using System;
using System.Linq.Expressions;
using Akka.Actor;

namespace AkkaLibrary.DataSynchronisation
{
    public class SampleAssembler : ReceiveActor
    {
        public SampleAssembler()
        {
            
        }

        private void CreateSampleBuilderExpressions()
        {
            var lambda = Expression.Lambda(
                Expression.Subtract(
                    Expression.Constant(1,typeof(int)),
                    Expression.Constant(2,typeof(int))
                    ));
            
            var sqrtMethod = typeof(Math).GetMethod("Sqrt", new[] { typeof(double) });
        }

        public static Props GetProps() => Props.Create(() => new SampleAssembler());        
    }
}