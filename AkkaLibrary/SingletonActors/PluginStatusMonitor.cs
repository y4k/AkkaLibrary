using Akka.Actor;

namespace AkkaLibrary
{
    public class PluginStatusMonitor : ReceiveActor
    {
        public PluginStatusMonitor()
        {
        }

        public static Props GetProps() => Props.Create(() => new PluginStatusMonitor());

        #region Messages

        #endregion
    }
}