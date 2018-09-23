using System;
using AkkaLibrary.Common.Configuration;
using AkkaLibrary.Common.Interfaces;
using AkkaLibrary.Common.Utilities;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace AkkaLibrary.Common.Logging
{
    /// <summary>
    /// Logging factory that creates an <see cref="ILogger"/> logging
    /// to the console and an elasticsearch instance
    /// </summary>
    public class ElasticSearchLoggerFactory : ILoggerFactory
    {
        /// <summary>
        /// The <see cref="LoggingLevelSwitch"/> corresponding
        /// to the console sink
        /// </summary>
        /// <returns></returns>
        public LoggingLevelSwitch ConsoleSwitch { get; }

        /// <summary>
        /// The <see cref="LoggingLevelSwitch"/> corresponding
        /// to the Elasticsearch sink
        /// </summary>
        /// <returns></returns>
        public LoggingLevelSwitch ElasticsearchSwitch { get; }

        /// <summary>
        /// Constructor with a given uri and the
        /// <see cref="SerilogConfig.DefaultMessageTemplate"/>
        /// </summary>
        /// <param name="uri"></param>
        public ElasticSearchLoggerFactory(Uri uri)
         : this(uri, SerilogConfig.DefaultMessageTemplate) { }

        /// <summary>
        /// Constructor with a given uri and a message template
        /// </summary>
        /// <param name="uri"></param>
        public ElasticSearchLoggerFactory(Uri uri, string messageTemplate)
        {
            _messageTemplate = messageTemplate;

            ConsoleSwitch = new LoggingLevelSwitch();
            ElasticsearchSwitch = new LoggingLevelSwitch();

            _settings = new ElasticsearchSinkOptions(uri)
            {
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6,
                IndexFormat = "serilog-index-{0:yyyy:MM.dd}",
                CustomFormatter = new ExceptionAsObjectJsonFormatter(renderMessage:true),
                LevelSwitch = ElasticsearchSwitch
            };

            _logger = new LoggerConfiguration()
                    .Enrich.WithProperty(LoggingExtensions.Identity, SerilogConfig.DefaultLoggerIdentity, true)
                    .WriteTo.Console(outputTemplate: _messageTemplate, levelSwitch:ConsoleSwitch, theme:SerilogConfig.DefaultConsoleTheme)
                    .WriteTo.Elasticsearch(_settings)
                    .CreateLogger();
        }

        /// <inheritdoc/>
        public ILogger GetLogger() => _logger;

        private readonly string _messageTemplate;
        private readonly ElasticsearchSinkOptions _settings;
        private readonly ILogger _logger;
    }
}