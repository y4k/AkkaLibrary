using AkkaLibrary.Common.Configuration;
using AkkaLibrary.Common.Interfaces;
using Serilog;
using Serilog.Core;

namespace AkkaLibrary.Common.Logging
{
    /// <summary>
    /// Factory that produces an <see cref="ILogger"/> logging to
    /// a file with the option to log to the console
    /// </summary>
    public class FileLoggerFactory : ILoggerFactory
    {
        /// <summary>
        /// The <see cref="LoggingLevelSwitch"/> corresponding
        /// to the console sink
        /// </summary>
        /// <returns></returns>
        public LoggingLevelSwitch ConsoleSwitch { get; } = new LoggingLevelSwitch();

        /// <summary>
        /// The <see cref="LoggingLevelSwitch"/> corresponding
        /// to the rolling file sink
        /// </summary>
        /// <returns></returns>
        public LoggingLevelSwitch RollingFileSwitch { get; } = new LoggingLevelSwitch();

        /// <summary>
        /// Constructor that uses the <see cref="SerilogConfig.DefaultMessageTemplate"/>
        /// </summary>
        /// <param name="logToConsole"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public FileLoggerFactory(bool logToConsole = true, string filePath = "LogFile-{Date}.log") : this(SerilogConfig.DefaultMessageTemplate) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logToConsole">Whether the logger should log to the console</param>
        /// <param name="messageTemplate">The message template</param>
        /// <param name="filePath">Location of the output file</param>
        public FileLoggerFactory(string messageTemplate, bool logToConsole = true, string filePath = "LogFile-{Date}.log")
        {
            _messageTemplate = messageTemplate;
            _filePath = filePath;

            var baseConfiguration = new LoggerConfiguration()
                        .WriteTo.RollingFile(_filePath, outputTemplate: _messageTemplate, levelSwitch: RollingFileSwitch);

            if(logToConsole)
            {
                baseConfiguration.WriteTo.Console(outputTemplate: _messageTemplate, theme:SerilogConfig.DefaultConsoleTheme, levelSwitch:ConsoleSwitch);
            }
            
            _logger = baseConfiguration.CreateLogger();
        }

        /// <inheritdoc/>
        public ILogger GetLogger() => _logger;
        
        private static string _messageTemplate;
        private static string _filePath;
        private readonly Logger _logger;

    }
}