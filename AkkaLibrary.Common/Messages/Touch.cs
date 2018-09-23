using System;
using Akka.Actor;
using AkkaLibrary.Common.Interfaces;

namespace AkkaLibrary.Common.Messages
{
    public class Touch : IConfirmable<Touch>
    {
        public Guid Id { get; } = Guid.NewGuid();

        public IConfirmation<Touch> GetConfirmation()
            => new TouchConfirmation(Id);

        IConfirmation IConfirmable.GetConfirmation() => GetConfirmation();

        public class TouchConfirmation : IConfirmation<Touch>
        {
            public TouchConfirmation(Guid id, string description = "")
            {
                ConfirmationId = id;
                Description = description;
            }
            public Guid ConfirmationId { get; }

            public string Description { get; }
        }
    }
}