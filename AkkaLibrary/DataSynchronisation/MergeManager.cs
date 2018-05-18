using Akka.Actor;

namespace AkkaLibrary.DataSynchronisation
{
    public class MergeManager : ReceiveActor
    {
        public MergeManager()
        {
            
        }

        public static Props GetProps() => Props.Create(() => new MergeManager());
    }
}