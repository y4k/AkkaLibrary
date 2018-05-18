using Akka.Actor;

namespace AkkaLibrary.Common.Interfaces
{
    /// <summary>
    /// Interface defining a plugin configuration.
    /// </summary>
    public interface IPluginConfiguration : IConfirmable<IPluginConfiguration>
    {
        /// <summary>
        /// Plugin name. Must be unique. Used as the name of the actor.
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        /// <see cref="Props"/> of the actor to create
        /// </summary>
        /// <returns></returns>
        Props ActorProps { get; }

        string[] SubcribeTopics { get; }
        
        string[] PublishTopics { get; }
    }
}