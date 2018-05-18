using Akka.Event;
using AkkaLibrary;
using AkkaLibrary.Common.Utilities;
using AkkaLibrary.ServiceScaffold;
using System.Collections.Generic;

namespace TestHarness
{
    public class TestPlugin : BasePluginActor<TestPlugin>
    {
        private readonly ILoggingAdapter _logger = Context.WithIdentity("TestPlugin");

        protected override bool Configure(BasePluginConfiguration<TestPlugin> configuration)
        {
            _logger.Info("Configuring.");
            return true;
        }

        protected override IEnumerable<DataTopic> GetInputTopics()
        {
            _logger.Info("Getting Input Topics.");
            return new DataTopic[] { };
        }

        protected override IEnumerable<DataTopic> GetOutputTopics()
        {
            _logger.Info("Getting Output Topics.");
            return new DataTopic[] { };
        }
    }
}
