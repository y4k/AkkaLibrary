using System;
using System.Collections.Generic;
using System.Reflection;
using Akka.Actor;

namespace AkkaLibrary.DataSynchronisation
{
    public class DataSynchroniserPluginActor : ReceiveActor
    {
        private Dictionary<string, IActorRef> _streamInputs = new Dictionary<string, IActorRef>();

        private IActorRef _mergeManager;
        private IActorRef _sampleAssembler;
        private DataSyncConfiguration _config;

        public DataSynchroniserPluginActor()
        {
            
        }

        private void Initialise()
        {
            Receive<DataSynchroniserMessages.Configure>(msg =>
            {
                _config = msg.Config;
            });
        }

        private void Working()
        {

        }

        private IEnumerable<IActorRef> CreateStreamInputActors()
        {
            return new List<IActorRef>();
        }

        private IActorRef CreateMergeManagerActor()
        {
            return Context.ActorOf(MergeManager.GetProps());
        }

        private IActorRef CreateSampleAssemblerActor()
        {
            return Context.ActorOf(SampleAssembler.GetProps());
        }

        public static Props GetProps() => Props.Create(() => new DataSynchroniserPluginActor());
    }

    public class DataSyncConfiguration
    {
        public List<DataStream> Streams { get; set; }
    }

    public class DataStream
    {
        public string Name { get; set; }
        public Type DataType { get; set; }
        public List<Transform> Transforms { get; set; }
    }

    public class Transform
    {
        public string Assignment { get; set; }
        public List<string> Extractions { get; set; }
    }
}