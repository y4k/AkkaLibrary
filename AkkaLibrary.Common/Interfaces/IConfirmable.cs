using System;
using Akka.Actor;

namespace AkkaLibrary.Common.Interfaces
{
    /// <summary>
    /// Interface that defines a class as confirmable and allows it to create a
    /// confirmation message.
    /// </summary>
    public interface IConfirmable<out T> : IConfirmable
    {
        /// <summary>
        /// Creates a confirmation message.
        /// </summary>
        /// <returns></returns>
        new IConfirmation<T> GetConfirmation();
    }

    public interface IConfirmable
    {
        /// <summary>
        /// Identity of the message.
        /// </summary>
        /// <returns></returns>
        Guid Id { get; }

        IConfirmation GetConfirmation();        
    }

    /// <summary>
    /// Interface for confirmation messages with a confirmation ID.
    /// </summary>
    public interface IConfirmation<out T> : IConfirmation
    {
    }

    public interface IConfirmation
    {
        /// <summary>
        /// The GUID confirmation ID.
        /// </summary>
        /// <returns></returns>
        Guid ConfirmationId { get; }

        /// <summary>
        /// Description of the message confirmed
        /// </summary>
        /// <returns></returns>
        string Description { get; }
    }

    /// <summary>
    /// Helper extensions for confirmable messages
    /// </summary>
    public static class ConfirmationExtensions
    {
        /// <summary>
        /// Automatically send confirmation message to the target
        /// for a given message
        /// </summary>
        /// <param name="confirmable"></param>
        /// <param name="target"></param>
        public static void Confirm<T>(this IConfirmable<T> confirmable, IActorRef target)
        {
            target.Tell(confirmable.GetConfirmation());
        }

        public static void Confirm(this IConfirmable confirmable, IActorRef target)
        {
            target.Tell(confirmable.GetConfirmation());
        }
    }
}