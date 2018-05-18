using AkkaLibrary.Common.Configuration;

namespace AkkaLibrary.Common.Interfaces
{
    /// <summary>
    /// Interface for specifying logging criteria in an Akka.NET <see cref="Config"/>
    /// </summary>
    public interface ILoggingConfig
    {
        /// <summary>
        /// Level at which messages will be logged.
        /// </summary>
        /// <returns><see cref="LogLevelEnum"/></returns>
        LogLevelEnum LogLevel { get; }

        /// <summary>
        /// The name of the class that will be created as the ActorSystem logging sink.
        /// </summary>
        /// <returns>The logger class name</returns>
        string LoggerClassName { get; }

        /// <summary>
        /// The name of the binary in which <see cref="LoggerClassName"/> resides.
        /// </summary>
        /// <returns>Binary name.</returns>
        string LoggerBinaryName { get; }
    }
}