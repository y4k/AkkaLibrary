using System;
using System.Net;

namespace AkkaLibrary
{
    public static class FpgaPluginMessages
    {
        public sealed class StreamComplete
        {
            
        }

        public sealed class Configure
        {
            public FpgaAcquisitionConfiguration Configuration { get; }

            public Configure(FpgaAcquisitionConfiguration config)
            {
                Configuration = config;
            }
        }

        public class ConnectionClosedException : Exception
        {
            public EndPoint EndPoint { get; }
            
            public ConnectionClosedException(EndPoint endpoint, string message) : base(message)
            {
                EndPoint = endpoint;
            }
        }
    }
}
