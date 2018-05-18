using System;

namespace AkkaLibrary.ServiceScaffold
{
    public class DataTopic
    {
        public string TopicName { get; }
        public Type DataType { get; }

        public DataTopic(string name, Type type)
        {
            TopicName = name;
            DataType = type;
        }
    }
}