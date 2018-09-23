using Akka.Actor;
using Akka.Event;
using Akka.Logger.Serilog;
using AkkaLibrary.Common.Interfaces;
using AkkaLibrary.Common.Logging;

namespace TestHarness
{
    public class LoggingActor : ReceiveActor
    {
        private readonly ILoggingAdapter _logger;

        public LoggingActor()
        {
            _logger = Context.WithIdentity("LoggingActor");

            Receive<ISyncData>(msg => _logger.Info("{TachoCount} - {TimedObject}", msg.TachometerCount, msg));
            ReceiveAny(msg => _logger.Info("{Received}", msg));
        }
    }
}