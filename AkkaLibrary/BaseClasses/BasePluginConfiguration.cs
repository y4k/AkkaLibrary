using Akka.Actor;

namespace AkkaLibrary.ServiceScaffold
{
    public abstract class BasePluginConfiguration<TPluginType> where TPluginType : BasePluginActor<TPluginType>, new()
    {
        public string Name { get; set; }
    }
}