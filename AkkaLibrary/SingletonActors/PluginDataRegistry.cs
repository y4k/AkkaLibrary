using Akka.Actor;

namespace AkkaLibrary
{
    public class PluginDataRegistry : ReceiveActor
    {
        public PluginDataRegistry()
        {

        }

        public static Props GetProps() => Props.Create(() => new PluginDataRegistry());

        #region Messages

        #endregion
    }
}