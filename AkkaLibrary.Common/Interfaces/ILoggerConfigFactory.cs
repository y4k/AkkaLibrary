using Serilog;

namespace AkkaLibrary.Common.Interfaces
{
    /// <summary>
    /// Defines an interface that produces a Serilog logger
    /// </summary>
    public interface ILoggerFactory
    {
        /// <summary>
        /// Returns the <see cref="ILogger"/>
        /// </summary>
        /// <returns><see cref="ILogger"/></returns>
        ILogger GetLogger();
    }
}