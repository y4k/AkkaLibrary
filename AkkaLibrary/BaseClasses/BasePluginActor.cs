using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using AkkaLibrary.ServiceScaffold;
using AkkaLibrary.Common.Interfaces;
using System;

namespace AkkaLibrary
{
    public abstract class BasePluginActor<TPluginType> : ReceiveActor, IPluginActor where TPluginType : BasePluginActor<TPluginType>, new()
    {
        public string Name { get; } = Context.Self.Path.Name;

        public Guid Id { get; } = Guid.NewGuid();

        protected BasePluginActor()
        {
            Receive<PluginSupervisorMessages.Configure<TPluginType>>(msg =>
            {
                if(Configure(msg.Config))
                {
                    Become(Ready);
                    Sender.Tell(new PluginSupervisorMessages.Configured());
                }
            });
        }

        private void Ready()
        {
            Receive<PluginSupervisorMessages.TopicRequest>(msg =>
            {
                var inputTopics = GetInputTopics();
                var outputTopics = GetOutputTopics();
                Sender.Tell(new PluginSupervisorMessages.TopicPublish(inputTopics, outputTopics), Self);
            });
        }

        protected abstract bool Configure(BasePluginConfiguration<TPluginType> configuration);

        protected abstract IEnumerable<DataTopic> GetInputTopics();

        protected abstract IEnumerable<DataTopic> GetOutputTopics();
    }
}