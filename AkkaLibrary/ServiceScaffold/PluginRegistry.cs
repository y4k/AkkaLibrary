using System;
using System.Collections.Generic;
using System.Linq;
using Akka;
using Akka.Actor;
using Akka.Event;
using AkkaLibrary.Common.Logging;
using AkkaLibrary.Common.Utilities;

namespace AkkaLibrary.ServiceScaffold
{

    public class PluginRegistry : ReceiveActor
    {
        private Dictionary<string, IActorRef> _plugins;
        private Dictionary<string, IEnumerable<DataTopic>> _pluginInputTopics;
        private Dictionary<string, IEnumerable<DataTopic>> _pluginOutputTopics;
        private readonly ILoggingAdapter _logger;

        public static Props GetProps() => Props.Create(() => new PluginRegistry());

        public PluginRegistry()
        {
            _plugins = new Dictionary<string, IActorRef>();
            _pluginInputTopics = new Dictionary<string, IEnumerable<DataTopic>>();
            _pluginOutputTopics = new Dictionary<string, IEnumerable<DataTopic>>();

            _logger = Context.WithIdentity("PluginRegsitry");

            Working();
        }

        private void Working()
        {
            Receive<RegistryMessages.RegisterPlugin>(msg =>
            {
                if(_plugins.ContainsKey(msg.ActorName))
                {
                    _logger.Info($"Actor {msg.ActorName} already registered.");
                }
                else
                {
                    _plugins.Add(msg.ActorName, Sender);
                    _pluginInputTopics.Add(msg.ActorName, msg.InputTopics);
                    _pluginOutputTopics.Add(msg.ActorName, msg.OutputTopics);
                    _logger.Info("Actor {0} has been registered with the following:{NewLine}Input topics:{NewLine}{1}.{NewLine}Output topics:{2}", msg.ActorName, string.Join("{NewLine}",_pluginInputTopics), string.Join("{NewLine}",_pluginOutputTopics));
                }
            });

            Receive<RegistryMessages.UnregisterPlugin>(msg =>
            {
                if(_plugins.ContainsKey(msg.ActorName))
                {
                    _plugins.Remove(msg.ActorName);
                    _pluginInputTopics.Remove(msg.ActorName);
                    _pluginOutputTopics.Remove(msg.ActorName);
                }
                _logger.Info($"Actor {msg.ActorName} is not registered and cannot be removed.");
            });

            // Receive<RegistryMessages.RequestAllTopics>(msg =>
            // {
            //     var topics = _plugins.Select(kvp => (kvp.Key, kvp.Value));
            //     Sender.Tell(new RegistryMessages.TopicList(topics));
            // });

            // Receive<RegistryMessages.RequestPluginTopics>(msg =>
            // {
            //     var topics = _plugins.Where(kvp => kvp.Key == msg.ActorName).Select(kvp => (kvp.Key, kvp.Value));
            //     Sender.Tell(new RegistryMessages.TopicList(topics));
            // });

            // Receive<RegistryMessages.RequestTopicType>(msg =>
            // {
            //     var topics = _plugins.Where(kvp => kvp.Value.GetType() == msg.TopicType).Select(kvp => (kvp.Key, kvp.Value));
            //     Sender.Tell(new RegistryMessages.TopicList(topics));
            // });
        }

        private void CheckPluginsForMatchingInputTopics(IEnumerable<DataTopic> inputTopics)
        {

        }

        private void CheckPluginsForMatchingOutputTopics()
        {
            
        }
    }

    public static class RegistryMessages
    {
        public sealed class RegisterPlugin
        {
            public string ActorName { get; }
            public IReadOnlyList<DataTopic> InputTopics { get; }
            public IReadOnlyList<DataTopic> OutputTopics { get; }

            public RegisterPlugin(string name, IEnumerable<DataTopic> inputs, IEnumerable<DataTopic> outputs)
            {
                ActorName = name;
                InputTopics = inputs.ToArray();
                OutputTopics = outputs.ToArray();
            }
        }

        public sealed class UnregisterPlugin
        {
            public string ActorName { get; }

            public UnregisterPlugin(string name)
            {
                ActorName = name;
            }
        }

        public sealed class RequestAllTopics { }
        

        public sealed class RequestTopicType
        {
            public Type TopicType { get; }

            public RequestTopicType(Type topicType)
            {
                TopicType = topicType;
            }
        }

        public sealed class RequestPluginTopics
        {
            public string ActorName { get; }

            public RequestPluginTopics(string name)
            {
                ActorName = name;
            }
        }

        public sealed class TopicList
        {
            public IReadOnlyList<(string name, IActorRef actor)> Topics { get; }

            public TopicList(IEnumerable<(string, IActorRef)> topics)
            {
                Topics = topics.ToArray();
            }
        }

        public sealed class TopicSubscribeRequest
        {
            public string ActorName { get; }
            public IActorRef ActorRef { get; }
            public string TopicName { get; }

            public TopicSubscribeRequest(string name, IActorRef actor, string topicName)
            {
                ActorName = name;
                TopicName = topicName;
                ActorRef = actor;
            }
        }

        public sealed class TopicUnsubscribeRequest
        {
            public string ActorName { get; }
            public IActorRef ActorRef { get; }
            public string TopicName { get; }

            public TopicUnsubscribeRequest(string name, IActorRef actor, string topicName)
            {
                ActorName = name;
                TopicName = topicName;
                ActorRef = actor;
            }
        }
    }
}