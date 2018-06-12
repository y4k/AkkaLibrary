using Akka.Actor;

namespace AkkaLibrary.Exceedances
{
    public class ExceedanceManager : ReceiveActor
    {
        public ExceedanceManager(ExceedanceConfiguration config)
        {
            
        }
    }

    public class CriterionActor : ReceiveActor
    {
        
    }

    public class LimitActor : ReceiveActor
    {

    }
}