using Akka.Actor;
using AkkaLibrary.Common.Configuration;
using AkkaLibrary.Common.Interfaces;
using AkkaLibrary.ServiceScaffold;
using Serilog;

namespace AkkaLibrary
{
    public static class SystemFactory
    {
        public static (IActorRef, ActorSystem) CreatePluginSystem(string actorSystemName, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.GetLogger();

            Log.Logger = logger;

            var system = ActorSystem.Create(actorSystemName, CommonConfigs.BasicConfig());

            var registry = system.ActorOf<PluginRegistry>("plugin-registry");

            return (registry,system);
        }
    }
}