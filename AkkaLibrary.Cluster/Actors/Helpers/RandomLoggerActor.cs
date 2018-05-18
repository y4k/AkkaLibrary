using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Event;
using AkkaLibrary.Common.Interfaces;
using AkkaLibrary.Common.Utilities;

namespace AkkaLibrary.Cluster.Actors.Helpers
{
    public class RandomLoggerActorConfiguration : IPluginConfiguration
    {
        public RandomLoggerActorConfiguration(string name, TimeSpan minPeriod, TimeSpan maxPeriod, params string[] logTopics)
        {
            Name = name;
            PublishTopics = logTopics;
            ActorProps = Props.Create(() => new RandomLoggerActor(name, minPeriod, maxPeriod, logTopics));
        }

        public string Name { get; }

        public Props ActorProps { get; }

        public string[] SubcribeTopics { get; } = new string[0];

        public string[] PublishTopics { get; }

        public Guid Id { get; } = Guid.NewGuid();

        public IConfirmation<IPluginConfiguration> GetConfirmation()
            => new RandomLoggerConfigurationConfirmation(Name, Id);

        IConfirmation IConfirmable.GetConfirmation() => GetConfirmation();

        private sealed class RandomLoggerConfigurationConfirmation : IConfirmation<RandomLoggerActorConfiguration>
        {
            public RandomLoggerConfigurationConfirmation(string name, Guid Id)
            {
                Description = name;
                ConfirmationId = Id;
            }
            public Guid ConfirmationId { get; }

            public string Description { get; }
        }
    }

    public class RandomLoggerActor : ReceiveActor
    {
        private readonly Random _random;
        private readonly string[] _logTopics;
        private readonly ILoggingAdapter _logger;
        private readonly TimeSpan _minPeriod;
        private readonly TimeSpan _maxPeriod;
        private readonly IActorRef _mediator = DistributedPubSub.Get(Context.System).Mediator;

        public RandomLoggerActor(string name, TimeSpan minPeriod, TimeSpan maxPeriod, params string[] logTopics)
        {
            _random = new Random();
            _logTopics = logTopics;
            _logger = Context.WithIdentity(name);
            _minPeriod = minPeriod;
            _maxPeriod = maxPeriod;


            Receive<LogSomething>(msg =>
            {
                var time = DateTime.UtcNow;

                var level = _random.Choose(Enum.GetValues(typeof(LogLevel)).Cast<LogLevel>());
                var template = "{Guid} {Value} {Time} {TimeSinceLastMessage}";
                var guid = Guid.NewGuid();
                var value = _random.Next();
                var timeDiff = (time - msg.Sent).TotalMilliseconds;


                _logger.Log(
                    level,
                    template,
                    guid,
                    value,
                    time,
                    timeDiff
                );

                _mediator.Tell(new Publish("Logs", $"{guid} {value} {time} {timeDiff}"));

                ScheduleNext();
            });

            ScheduleNext();
        }

        private void ScheduleNext()
        {
            Context.System.Scheduler.ScheduleTellOnce(
                TimeSpan.FromMilliseconds(_random.Next((int)_minPeriod.TotalMilliseconds, (int)_maxPeriod.TotalMilliseconds)),
                Self, new LogSomething(), Self);
        }

        private sealed class LogSomething
        {
            public DateTime Sent { get; } = DateTime.UtcNow;
        }
    }

    public static class RandomExtensions
    {
        public static T Choose<T>(this Random random, params T[] values)
        {
            return random.Choose(values);
        }

        public static T Choose<T>(this Random random, IEnumerable<T> values)
        {
            return values.ElementAt(random.Next(0, values.Count()));
        }
    }
}