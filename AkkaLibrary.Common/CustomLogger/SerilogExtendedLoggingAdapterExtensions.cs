using System;
using Akka.Actor;
using Akka.Event;

namespace AkkaLibrary.Common.CustomLogger
{
    public static class SerilogExtendedLoggingAdapterExtensions
    {
        /// <summary>
        /// Create a logger that enriches log events with the specified property.
        /// </summary>
        /// <param name="adapter">ILoggingAdapter instance</param>
        /// <param name="propertyName">The name of the property. Must be non-empty.</param>
        /// <param name="value">The property value.</param>
        /// <param name="destructureObjects">If true, the value will be serialized as a structured object if possible; if false, the object will be recorded as a scalar or simple array.</param>
        public static ILoggingAdapter ForContext(this ILoggingAdapter adapter, string propertyName, object value, bool destructureObjects = false)
        {
            var customAdapter = adapter as SerilogExtendedLoggingAdapter;
            return customAdapter == null ? adapter : customAdapter.SetContextProperty(propertyName, value, destructureObjects);
        }

        /// <summary>
        /// Creates a new logging adapter using the specified context's event stream.
        /// </summary>
        /// <param name="context">The context used to configure the logging adapter.</param>
        /// <returns>The newly created logging adapter.</returns>
        public static ILoggingAdapter GetSerilogExtendedLogger(this IActorContext context)
        {
            var logSource = context.Self.ToString();
            var logClass = context.Props.Type;

            return Activator.CreateInstance(
                typeof(SerilogExtendedLoggingAdapter),
                new BusLogging(
                    context.System.EventStream,
                    logSource,
                    logClass,
                    new DefaultLogMessageFormatter()
                    )
                ) as SerilogExtendedLoggingAdapter;
        }
    }
}