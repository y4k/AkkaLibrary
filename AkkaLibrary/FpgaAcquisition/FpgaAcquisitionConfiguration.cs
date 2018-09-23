using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.IO;

namespace AkkaLibrary
{
    public class FpgaAcquisitionConfiguration : IFpgaConnectionConfig, IFpgaFramingConfig, IFpgaAssemblerConfig
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public List<FpgaChannel> ChannelList { get; set; }
        public int SamplesPerPacket { get; set; }
        public ByteString Delimiter { get; set; }
        public int MaxFrameLength { get; set; }
        public IActorRef OutputTarget { get; set; }
        public TimeSpan RetryConnectionTimeout { get; set; }
    }

    internal interface IFpgaAssemblerConfig
    {
        int SamplesPerPacket { get; set; }
        
        List<FpgaChannel> ChannelList { get; set; }
        
        IActorRef OutputTarget { get; set; }
    }

    internal interface IFpgaFramingConfig
    {
        ByteString Delimiter { get; set; }
        int MaxFrameLength { get; set; }        
    }

    public interface IFpgaConnectionConfig
    {
        string IpAddress { get; set; }
        int Port { get; set; }
        TimeSpan RetryConnectionTimeout { get; set; }
    }
}
