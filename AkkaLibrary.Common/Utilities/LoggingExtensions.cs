using Akka.Actor;
using Akka.Event;
using Akka.Logger.Serilog;

namespace AkkaLibrary.Common.Utilities
{
    /// <summary>
    /// Logging extensions for the Actor System to add property enrichment.
    /// </summary>
    public static class LoggingExtensions
    {
        #region Enable Serilog

        /// <summary>
        /// Gets the Serilog specific logger that enables Serilog-style context enrichment
        /// and gives access to the
        /// <see cref="SerilogLoggingAdapterExtensions.ForContext(ILoggingAdapter, string, object, bool)"/>
        /// extension method
        /// </summary>
        /// <param name="actorContext"></param>
        /// <returns></returns>
        public static ILoggingAdapter WithSerilog(this IActorContext actorContext)
        {
            return actorContext.GetLogger<SerilogLoggingAdapter>();
        }

        #endregion

        #region Actor Context Extensions

        /// <summary>
        /// Assigns a string to the Identity logging property
        /// </summary>
        /// <param name="actorContext">An Actor Context</param>
        /// <param name="identity">Identity string</param>
        /// <returns></returns>
        public static ILoggingAdapter WithIdentity(this IActorContext actorContext, string identity)
        {
            return actorContext.WithSerilog().WithIdentity(identity);
        }
        
        #endregion

        #region Logging Adapter Extensions

        /// <summary>
        /// Assigns a string to the Identity logging property
        /// </summary>
        /// <param name="actorContext">An Actor Context</param>
        /// <param name="identity">Identity string</param>
        /// <returns></returns>
        public static ILoggingAdapter WithIdentity(this ILoggingAdapter loggingAdapter, string identity)
        {
            return loggingAdapter.ForContext(Identity, identity, true);
        }

        #endregion

        #region Property Constants

        public static readonly string Identity = "Identity";
            
        #endregion
    }
}