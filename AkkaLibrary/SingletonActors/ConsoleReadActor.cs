using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using AkkaLibrary.Common.Utilities;

namespace AkkaLibrary.Streams
{
    public class ConsoleReadActor : ReceiveActor, IWithUnboundedStash
    {
        private int _promptIndex;
        private readonly ILoggingAdapter _logger;

        public ConsoleReadActor()
        {
            _logger = Context.WithIdentity("ConsoleReadActor");
            Waiting();
        }
        
        private void Waiting()
        {
            Receive<RequestConsoleInput>(msg =>
            {
                var requestingActor = Sender;
                Console.Write($"[{_promptIndex++}] {msg.Prompt}{Environment.NewLine}>>> ");
                Console.In
                .ReadLineAsync()
                .ContinueWith<object>(x =>
                {
                    if(x.IsCanceled || x.IsFaulted)
                    {
                        return new ConsoleInputFault(requestingActor, msg.Prompt);
                    }
                    return new ConsoleInput(requestingActor, x.Result, msg.Prompt);
                },TaskContinuationOptions.AttachedToParent &
                  TaskContinuationOptions.ExecuteSynchronously
                )
                .PipeTo(Self);

                BecomeStacked(Reading);
            });
        }

        private void Reading()
        {
            /*
            Stash all request for console input
            messages until reading has finished.
             */
            Receive<RequestConsoleInput>(msg =>
            {
                //Stash all requests while one is running.
                Stash.Stash();
            });

            Receive<ConsoleInputFault>(msg =>
            {
                //Send the recorded console input to
                //the original sender and revert to waiting for messages
                _logger.Error("Failed to acquire input for [{}] with original message [{}]", msg.RequestingActor, msg.OriginalMessage);
                UnbecomeStacked();                
                //Unstash any waiting messages.
                Stash.UnstashAll();
            });

            Receive<ConsoleInput>(msg =>
            {
                //Send the recorded console input to
                //the original sender and revert to waiting for messages
                msg.ReplyActor.Tell(new ConsoleRequestReply(msg.OriginalMessage, msg.Input));
                UnbecomeStacked();
                //Unstash any waiting messages.
                Stash.UnstashAll();
            });
        }

        protected override void PostStop()
        {
            Stash.ClearStash();
            base.PostStop();
        }

        public IStash Stash { get; set; }

        public static Props GetProps() => Props.Create(() => new ConsoleReadActor());

        #region Messages

        public sealed class RequestConsoleInput
        {
            public string Prompt { get; }

            public RequestConsoleInput(string prompt)
            {
                Prompt = prompt;
            }
        }

        private sealed class ConsoleInput
        {
            public IActorRef ReplyActor { get; }
            public string Input { get; }

            public string OriginalMessage { get; }

            public ConsoleInput(IActorRef replyActor, string input, string originalMessage)
            {
                ReplyActor = replyActor;
                Input = input;
                OriginalMessage = originalMessage;
            }
        }

        private sealed class ConsoleInputFault
        {
            public IActorRef RequestingActor { get; }
            public string OriginalMessage { get; }

            public ConsoleInputFault(IActorRef requestingActor, string originalMessage)
            {
                RequestingActor = requestingActor;
                OriginalMessage = originalMessage;                
            }
        }

        public sealed class ConsoleRequestReply
        {
            public string OriginalMessage { get; }
            public string Input { get; }

            public ConsoleRequestReply(string originalMessage, string input)
            {
                OriginalMessage = originalMessage;
                Input = input;
            }
        }

        #endregion
    }
}