using Akka.Actor;

namespace AkkaLibrary.Common.Interfaces
{
    /// <summary>
    /// Interface for a factory that creates a <see cref="Props"/> object.
    /// </summary>
    public interface IPropsFactory
    {
        /// <returns><see cref="Props"/></returns>
        Props CreateProps();
    }
}