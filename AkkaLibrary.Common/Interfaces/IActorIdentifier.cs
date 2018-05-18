using System;

namespace AkkaLibrary.Common.Interfaces
{
    public interface IActorIdentifier
    {
        string Name { get; }
        Guid Id { get; }
    }
}