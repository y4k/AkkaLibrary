using AkkaLibrary.Common.Configuration;
using AkkaLibrary.Common.Interfaces;
using Serilog;
using Serilog.Core;

namespace AkkaLibrary.Common.Logging
{
    /// <summary>
    /// Factory that produces an <see cref="ILogger"/> that
    /// logs to the console
    /// </summary>
    public class DefaultLoggerConfigFactory : ILoggerFactory
    {
        /// <summary>
        /// The <see cref="LoggingLevelSwitch"/> corresponding
        /// to the console sink
        /// </summary>
        /// <returns></returns>
        public LoggingLevelSwitch ConsoleSwitch { get; }

        /// <summary>
        /// Default constructor that uses the <see cref="SerilogConfig.DefaultMessageTemplate"/>
        /// </summary>
        /// <returns></returns>
        public DefaultLoggerConfigFactory() : this(SerilogConfig.DefaultMessageTemplate) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messageTemplate">The output message template</param>
        public DefaultLoggerConfigFactory(string messageTemplate)
        {
            _messageTemplate = messageTemplate;

            ConsoleSwitch = new LoggingLevelSwitch();

            _logger = new LoggerConfiguration()
                    .WriteTo.Console(
                        outputTemplate: _messageTemplate,
                        theme:SerilogConfig.DefaultConsoleTheme,
                        levelSwitch:ConsoleSwitch
                        )
                    .CreateLogger();
        }

        /// <inheritdoc/>
        public ILogger GetLogger() => _logger;

        private readonly string _messageTemplate;

        private readonly ILogger _logger;
    }
}