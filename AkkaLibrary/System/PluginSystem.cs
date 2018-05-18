using System.Collections.Generic;
using Akka.Actor;

namespace AkkaLibrary.ServiceScaffold
{
    public class PluginSystem
    {
        public readonly static string PluginRegistryName = "plugin-registry";

        public IActorRef PluginRegistryRef { get; private set; }

        public PluginSystem(ActorSystem system)
        {
            System = system;
            PluginRegistryRef = System.ActorOf(PluginRegistry.GetProps(), PluginRegistryName);
        }
        
        public ActorSystem System { get; private set; }

        public IActorRef CreatePlugin<TPluginType>(BasePluginConfiguration<TPluginType> configuration) where TPluginType : BasePluginActor<TPluginType>, new()
        {
            var props = PluginSupervisorActor<TPluginType>.GetProps(configuration, PluginRegistryRef);

            var name = $"{configuration.Name}-plugin-supervisor";

            var pluginSupervisor = System.ActorOf(props, name);

            pluginSupervisor.Tell(new PluginSupervisorMessages.PluginStart());

            return pluginSupervisor;
        }

        private bool ValidateConfiguration<TPluginType>(BasePluginConfiguration<TPluginType> config) where TPluginType : BasePluginActor<TPluginType>, new()
        {
            var errors = new List<string>();
            
            if(string.IsNullOrEmpty(config.Name))
            {
                errors.Add($"Name must not be null or empty.");
            }

            return errors.Count > 0 ? true : false;
        }
    }
}