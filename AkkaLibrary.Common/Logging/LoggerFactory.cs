using System.Collections.Generic;
using Akka.Actor;
using Akka.Logger.Serilog;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

namespace AkkaLibrary.Common.Logging
{
    public class LoggerFactory
    {
        #region Singleton Implementation
        
        private readonly LoggingLevelSwitch _loggingSwitch;
        private readonly Logger _logger;
        private LoggerFactory()
        {
            _loggingSwitch = new LoggingLevelSwitch();

            _logger = new LoggerConfiguration()
                    .WriteTo.Console(
                        outputTemplate: "[{Level:u3}][{Timestamp:dd/MM/yyyy HH:mm:ss}][{Identity}] {Message:lj}{NewLine}{Exception}",
                        theme:DefaultConsoleTheme
                        )
                    .MinimumLevel.ControlledBy(_loggingSwitch)
                    .Enrich.WithProperty("Identity", "SystemLogger")
                    .CreateLogger();
        }

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


        #endregion

        #region Static Methods

        private static LoggerFactory _instance = null;
        private static LoggerFactory Instance => _instance == null ? _instance = new LoggerFactory() : _instance;
        public static LoggingLevelSwitch LoggingSwitch => Instance._loggingSwitch;
        public static ILogger Logger => Instance._logger;

        #endregion
    }
}