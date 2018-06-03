using System;
using Akka.Actor;
using AkkaLibrary.Common.Logging;
using AkkaLibrary.Common.Utilities;

namespace AkkaLibrary.Actors
{
    public class EchoActor : ReceiveActor
    {
        public EchoActor()
        {
            ReceiveAny(msg =>
            {
                Console.WriteLine("Received {0} from {1} on {2}", msg, Sender.Path, Self.Path);
                Context.WithIdentity(Self.Path.Name).Info("Received {Item} from {Sender} on {Self}", msg, Sender.Path, Self.Path.Address);
            });
        }
    }
}