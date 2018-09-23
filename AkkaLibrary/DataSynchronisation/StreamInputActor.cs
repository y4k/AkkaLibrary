using System;
using Akka.Actor;

namespace AkkaLibrary.DataSynchronisation
{
    /// <summary>
    /// An input actor for the Data Synchronisation Plugin. Expects data
    /// from a specific sender of a specific type. Will ignore other data.
    /// </summary>
    public class StreamInputActor<T> : ReceiveActor
    {
        private readonly IActorRef _sender;
        private readonly Type _dataType;
        private readonly IActorRef _target;
        private readonly TimeSpan _streamTimeout;
        private ICancelable _timerMessage;

        public StreamInputActor(string streamName, IActorRef sender, IActorRef target, TimeSpan streamTimeout)
        {
            _sender = sender;
            _dataType = typeof(T);
            _target = target;
            _streamTimeout = streamTimeout;
        }

        private void Ready()
        {
            Receive<T>(msg =>
            {
                // Exit if the sender is not as expected.
                if(Sender == _sender)
                    Process(msg);
            });

            Receive<StreamTimeoutMessage>(msg =>
            {
                // Stream has timed out.
                _target.Tell(new StreamTimeoutMessage());
                ResetTimer();
            });
        }

        private void Process(T msg)
        {
            _target.Tell(msg);
            ResetTimer();
        }

        private void ResetTimer()
        {
            var self = Self;
            _timerMessage?.Cancel();
            _timerMessage = Context.System.Scheduler.ScheduleTellOnceCancelable(_streamTimeout, self, new StreamTimeoutMessage(), self);
        }

        public static Props GetProps<DataType>(string streamName, IActorRef sender, IActorRef target, TimeSpan streamTimeout)
            => Props.Create(() => new StreamInputActor<DataType>(streamName, sender, target, streamTimeout));

        public sealed class StreamTimeoutMessage { }
    }
}