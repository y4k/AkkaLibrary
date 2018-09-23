using System.Linq;
using Akka.Actor;
using Akka.Dispatch;
using Akka.Event;
using Serilog;
using Serilog.Core.Enrichers;

namespace AkkaLibrary.Common.CustomLogger
{
    public class SerilogExtendedLogger : ReceiveActor, IRequiresMessageQueue<ILoggerMessageQueueSemantics>
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger();

        public SerilogExtendedLogger()
        {
            Receive<Debug>(msg => HandleLogEvent(msg));
            Receive<Warning>(msg => HandleLogEvent(msg));
            Receive<Info>(msg => HandleLogEvent(msg));
            Receive<Error>(msg => HandleLogEvent(msg));
            Receive<InitializeLogger>(msg =>
            {
                _logger.Info("SerilogExtendedLogger started");
                Sender.Tell(new LoggerInitialized());
            });
        }

        private void HandleLogEvent(Debug logEvent)
        {
            GetLogger(logEvent).Debug(GetFormat(logEvent.Message), GetArgs(logEvent.Message));
        }

        private void HandleLogEvent(Warning logEvent)
        {
            GetLogger(logEvent).Warning(GetFormat(logEvent.Message), GetArgs(logEvent.Message));
        }
        
        private void HandleLogEvent(Error logEvent)
        {
            GetLogger(logEvent).Error(logEvent.Cause, GetFormat(logEvent.Message), GetArgs(logEvent.Message));
        }
        
        private void HandleLogEvent(Info logEvent)
        {
            GetLogger(logEvent).Information(GetFormat(logEvent.Message), GetArgs(logEvent.Message));
        }

        private static ILogger GetLogger(LogEvent logEvent) {
            var logger = Log.Logger.ForContext("SourceContext", Context.Sender.Path);
            
            
            
            logger = logger
                .ForContext("Timestamp", logEvent.Timestamp)
                .ForContext("LogSource", logEvent.LogSource)
                .ForContext("Thread", logEvent.Thread.ManagedThreadId.ToString().PadLeft(4, '0'));

            var logMessage = logEvent.Message as LogMessage;
            if (logMessage != null)
            {
                logger = logMessage.Args.OfType<PropertyEnricher>().Aggregate(logger, (current, enricher) => current.ForContext(enricher));
            }

            return logger;
        }
        
        private static string GetFormat(object message)
        {
            var logMessage = message as LogMessage;
            return logMessage != null ? logMessage.Format : "{Message:l}";
        }

        private static object[] GetArgs(object message)
        {
            var logMessage = message as LogMessage;
            return logMessage?.Args.Where(a => !(a is PropertyEnricher)).ToArray() ?? new[] { message };
        }
        
    }
}