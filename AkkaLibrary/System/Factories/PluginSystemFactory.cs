using Akka.Actor;
using Akka.Configuration;
using AkkaLibrary.Common.Configuration;

namespace AkkaLibrary.ServiceScaffold
{
    public static class PluginSystemFactory
    {
        public static PluginSystem NewPluginSystem(string systemName)
        {
            var config = CommonConfigs.BasicConfig();
            return NewPluginSystem(systemName, config);
        }

        public static PluginSystem NewPluginSystem(string systemName, Config config)
        {
            var actorSystem = ActorSystem.Create(systemName, config);
            return new PluginSystem(actorSystem);
        }
    }
}