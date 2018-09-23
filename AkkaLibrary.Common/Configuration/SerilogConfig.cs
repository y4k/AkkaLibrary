using Serilog.Sinks.SystemConsole.Themes;
using AkkaLibrary.Common.Interfaces;
using System.Collections.Generic;

namespace AkkaLibrary.Common.Configuration
{
    /// <summary>
    /// Implementation of <see cref="ILoggingConfig"/> that creates a Serilog logging configuration.
    /// </summary>
    public class SerilogConfig : ILoggingConfig
    {
        /// <summary>
        /// Creates an instance of the Serilog configuration.
        /// </summary>
        /// <param name="loglevel">Defaults to Information</param>
        public SerilogConfig(LogLevelEnum loglevel = LogLevelEnum.INFO)
        {
            LogLevel = loglevel;
        }

        /// <inheritdoc/>
        public LogLevelEnum LogLevel { get; }

        /// <inheritdoc/>
        public string LoggerClassName => "Akka.Logger.Serilog.SerilogLogger";

        /// <inheritdoc/>
        public string LoggerBinaryName => "Akka.Logger.Serilog";

        /// <summary>
        /// Default console theme for Serilog Console logging
        /// </summary>
        /// <returns></returns>
        public readonly static AnsiConsoleTheme DefaultConsoleTheme = new AnsiConsoleTheme(
            new Dictionary<ConsoleThemeStyle, string>
            {
            [ConsoleThemeStyle.Text] = "\x1b[38;5;0015m",
            [ConsoleThemeStyle.SecondaryText] = "\x1b[38;5;0246m",
            [ConsoleThemeStyle.TertiaryText] = "\x1b[38;5;0008m",
            [ConsoleThemeStyle.Invalid] = "\x1b[38;5;0011m",
            [ConsoleThemeStyle.Null] = "\x1b[38;5;0038m",
            [ConsoleThemeStyle.Name] = "\x1b[38;5;0081m",
            [ConsoleThemeStyle.String] = "\x1b[38;5;0216m",
            [ConsoleThemeStyle.Number] = "\x1b[38;5;0200m",
            [ConsoleThemeStyle.Boolean] = "\x1b[38;5;0027m",
            [ConsoleThemeStyle.Scalar] = "\x1b[38;5;0079m",
            [ConsoleThemeStyle.LevelVerbose] = "\x1b[38;5;0007m",
            [ConsoleThemeStyle.LevelDebug] = "\x1b[38;5;0007m",
            [ConsoleThemeStyle.LevelInformation] = "\x1b[37;4m",
            [ConsoleThemeStyle.LevelWarning] = "\x1b[33;1m",
            [ConsoleThemeStyle.LevelError] = "\x1b[38;5;0197m\x1b[48;5;0238m",
            [ConsoleThemeStyle.LevelFatal] = "\x1b[38;5;0015m\x1b[48;5;0196m",
            });

        public readonly static string DefaultMessageTemplate
            = "[{Timestamp:HH:mm:ss} {Level:u3}] [{Identity}] {Message:lj}{NewLine}{Exception}";

        public readonly static string DefaultLoggerIdentity = "SystemLogger";
    }
}