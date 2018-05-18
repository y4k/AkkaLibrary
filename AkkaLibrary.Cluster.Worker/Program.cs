using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Cluster.Tools.Singleton;
using AkkaLibrary.Cluster.Actors;
using AkkaLibrary.Cluster.Configuration;
using AkkaLibrary.Common.Configuration;
using AkkaLibrary.Common.Logging;
using AkkaLibrary.Common.Utilities;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace AkkaLibrary.Cluster.Worker
{
    class Program
    {
        private static string[] RequiredRoles = new[] { "manager", "configurator" };

        static void Main(string[] args)
        {
            var numArgs = 2;

            if (args.Length < numArgs)
            {
                Log.Fatal("Cluster Bootstrapper does not have the correct number of arguments. Expected at least {ExpectedArguments} but received {NumberOfArguments}", numArgs, args.Length);
                var count = 1;
                foreach (var arg in args)
                {
                    Log.Information("Arg[{ArgumentIndex}]:{Argument}",count,arg);
                    Log.CloseAndFlush();
                }
                return;
            }

            var loggerFactory = new ElasticSearchLoggerFactory(new Uri("http://localhost:9200"));
            var consoleSwitch = loggerFactory.ConsoleSwitch;
            var elasticSwitch = loggerFactory.ElasticsearchSwitch;

            var bootstrapperIdentity = args[0];
            var roles = args.Skip(1).ToList();

            foreach (var item in RequiredRoles)
            {
                if(!roles.Contains(item))
                {
                    roles.Add(item);
                }
            }

            Log.Logger = loggerFactory.GetLogger().ForContext(LoggingExtensions.Identity, $"SystemLogger-{bootstrapperIdentity}", true);

            var logger = loggerFactory.GetLogger().ForContext(LoggingExtensions.Identity, bootstrapperIdentity);

            logger.Information("Cluster Worker started");
            logger.Information("Worker started with roles:{ClusterRoles}", roles);

            var systemName = "actor-system";
            var hostname = "127.0.0.1";

            var seedNode = $"akka.tcp://{ClusterNodeConstants.DefaultSystemName}@{hostname}:4053";

            // Create the system and actors
            var cfg = CommonConfigs.BasicConfig()// Supress JSON warning
                .WithFallback(ClusterNodeConstants.DefaultConfig()) // With default actor props factory
                .WithFallback(
                    ClusterConfigs.CreateClusterConfig(
                        new DefaultClusterConfig(
                            hostname, 0,
                            seedNodes: new[] { seedNode },
                            roles: roles.Select(x => KeyValuePair.Create(x, 0)).ToDictionary(x => x.Key, x => x.Value)
                            )
                        )
                    )
                .WithFallback(CommonConfigs.CreateLoggingConfig(new SerilogConfig())); // Serilog logging

            var system = ActorSystem.Create(systemName, cfg);
            var configurator = system.ActorOf(Props.Create(() => new NodeConfigurator()), "configurator");

            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                Console.WriteLine("Stopping Cluster Worker...");
                await CoordinatedShutdown.Get(system).Run();
            };

            // Waits until service is terminated and then exits the program
            system.WhenTerminated.Wait();

            logger.Information("Cluster Worker stopped");
            
            Log.CloseAndFlush();
        }
    }
}