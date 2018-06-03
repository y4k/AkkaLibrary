using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using AkkaLibrary.Common.Logging;

namespace AkkaLibrary.ServiceScaffold
{
    public sealed class PluginSupervisorActor<TPluginType> : ReceiveActor where TPluginType : BasePluginActor<TPluginType>, new()
    {
        private Dictionary<string, Type> _inputs;
        private Dictionary<string, Type> _outputs;
        private Dictionary<string, IActorRef> _subscriberRefs;
        private Dictionary<string, IEnumerable<string>> _subscriberTopics;
        
        private readonly Props _pluginProps;
        private readonly BasePluginConfiguration<TPluginType> _pluginConfig;
        
        private readonly IActorRef _registry;
        private string _name;
        private readonly ILoggingAdapter _logger;
        private IActorRef _pluginRef;

        public PluginSupervisorActor(BasePluginConfiguration<TPluginType> pluginConfig, IActorRef registry)
        {
            _inputs = new Dictionary<string, Type>();
            _outputs = new Dictionary<string, Type>();
            _subscriberRefs = new Dictionary<string, IActorRef>();
            _subscriberTopics = new Dictionary<string, IEnumerable<string>>();

            _pluginProps = Props.Create(() => new TPluginType());
            _pluginConfig = pluginConfig;

            _registry = registry;
            _name = Context.Self.Path.Name;

            _logger = Context.WithIdentity("PluginSupervisor");

            Ready();
        }

        public static Props GetProps(BasePluginConfiguration<TPluginType> configuration, IActorRef registry)
        {
            return Props.Create(() => new PluginSupervisorActor<TPluginType>(configuration, registry));
        }

        #region States

        private void Ready()
        {
            //Initialised and sending service discovery messages before starting launch.
            _logger.Info("Ready");

            Receive<PluginSupervisorMessages.PluginStart>(msg =>
            {
                _pluginRef = Context.ActorOf(_pluginProps, _pluginConfig.Name);
                _pluginRef.Tell(new PluginSupervisorMessages.Configure<TPluginType>(_pluginConfig));
            });

            Receive<PluginSupervisorMessages.Configured>(msg =>
            {
                Sender.Tell(new PluginSupervisorMessages.TopicRequest());
            });

            Receive<PluginSupervisorMessages.TopicPublish>(msg =>
            {
                _inputs = msg.InputTopics.Select(dt => KeyValuePair.Create(dt.TopicName, dt.DataType)).ToDictionary(source => source.Key, source => source.Value);
                _outputs = msg.OutputTopics.Select(dt => KeyValuePair.Create(dt.TopicName, dt.DataType)).ToDictionary(source => source.Key, source => source.Value);
                
                _registry.Tell(new RegistryMessages.RegisterPlugin(_name, msg.InputTopics, msg.OutputTopics));
            });
        }
        private void Launching()
        {
            _logger.Info("Launching");

            //Launching state where it is attemping to gather the necessary resources.

            

            Receive<RegistryMessages.TopicList>(msg =>
            {

            });
        }
        private void Running()
        {
            _logger.Info("Running");

            //State after launching. All necessary topics are found and the plugin is running.
            //Child actors have been created.
        }
        private void Stopping()
        {
            _logger.Info("Stopping.");

            //Something has triggered the shutdown of this plugin. Topics may have been changed etc
            //May move back to ready or stopped.
        }
        private void Stopped()
        {
            _logger.Info("Stopped.");

            //May not be necessary. Seems equivalent to doing nothing or ready. If nothing then
            //the actor serves no purpose. If ready then this is redundant
        }

        #endregion

        private void HandleRegistryMessage()
        {
            Receive<RegistryMessages.TopicSubscribeRequest>(msg =>
            {

            });

            Receive<RegistryMessages.TopicUnsubscribeRequest>(msg =>
            {

            });
        }
    }

    public static class PluginSupervisorMessages
    {
        public sealed class PluginStart { }
        public sealed class TopicRequest { }

        public sealed class TopicPublish
        {
            public IReadOnlyList<DataTopic> InputTopics { get; }
            public IReadOnlyList<DataTopic> OutputTopics { get; }

            public TopicPublish(IEnumerable<DataTopic> inputTopics, IEnumerable<DataTopic> outputTopics)
            {
                InputTopics = inputTopics.ToArray();
                OutputTopics = outputTopics.ToArray();
            }
        }

        public sealed class Configured { }

        public sealed class Configure<TPluginType> where TPluginType : BasePluginActor<TPluginType>, new()
        {
            public BasePluginConfiguration<TPluginType> Config { get; set; }
            public Configure(BasePluginConfiguration<TPluginType> configuration)
            {
                
            }
        }
    }
}