using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Cluster.Tools.Singleton;
using AkkaLibrary.Cluster.Actors;
using AkkaLibrary.Cluster.Actors.Helpers;
using AkkaLibrary.Cluster.Configuration;
using AkkaLibrary.Common.Configuration;
using AkkaLibrary.Common.Logging;
using AkkaLibrary.Common.Utilities;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace AkkaLibrary.Cluster.Manager
{
    class Program
    {
        static void Main()
        {
            var loggerFactory = new ElasticSearchLoggerFactory(new Uri("http://localhost:9200"));
            var consoleSwitch = loggerFactory.ConsoleSwitch;
            var elasticSwitch = loggerFactory.ElasticsearchSwitch;

            Log.Logger = loggerFactory.GetLogger();

            var bootstrapperIdentity = "Client";
            var roles = new Dictionary<string, int>
            {
                { "client", 0}
            };

            var logger = loggerFactory.GetLogger().ForContext(LoggingExtensions.Identity, bootstrapperIdentity, true);
            
            logger.Information("Cluster Bootstrapper started");

            var hostname = "127.0.0.1";
            var port = 4321;
            var systemName = "client-actor-system";

            var selfSeedNode = $"akka.tcp://{systemName}@{hostname}:{port}";

            // Create the system and actors
            var cfg = CommonConfigs.BasicConfig()// Supress JSON warning
                .WithFallback(ClusterNodeConstants.DefaultConfig()) // With default actor props factory
                .WithFallback(
                    ClusterConfigs.CreateClusterConfig(
                        new DefaultClusterConfig(
                            hostname, port, // hostname and any port
                            seedNodes: new [] { selfSeedNode  }, // Seed nodes
                            roles: roles
                            )
                        )
                    )
                .WithFallback(CommonConfigs.CreateLoggingConfig(new SerilogConfig())); // Serilog logging

            var system = ActorSystem.Create(systemName, cfg); // 
            
            // Create configurations
            var workerNodeConfig = new SystemConfiguration(
                new RandomLoggerActorConfiguration("LoggerOne", TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(3), "Logs"),
                new RandomLoggerActorConfiguration("LoggerTwo", TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(3), "Logs"),
                new RandomLoggerActorConfiguration("LoggerThree", TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(3), "Logs"),
                new RandomLoggerActorConfiguration("LoggerFour", TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(3), "Logs"),
                new RandomLoggerActorConfiguration("LoggerFive", TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(3), "Logs")
                );

            var listenerNodeConfig = new SystemConfiguration(new LogConfirmationActorConfiguration("ListenerPlugin", "Logs"));

            
            
            var targetNode = $"akka.tcp://{ClusterNodeConstants.DefaultSystemName}@{hostname}:4053";
            var clientSettings = ClusterClientSettings.Create(system)
                                .WithInitialContacts(
                                    new[]
                                    {
                                        ActorPath.Parse(targetNode + "/system/receptionist")
                                    }.ToImmutableHashSet()
                                    );

            var clusterClient = system.ActorOf(ClusterClient.Props(clientSettings), "cluster-client");
            
            // Publishes to given topic
            clusterClient.Tell(new ClusterClient.Publish("client-messages", new ConfigureRoles(workerNodeConfig, "worker")));
            clusterClient.Tell(new ClusterClient.Publish("client-messages", new ConfigureRoles(listenerNodeConfig, "listener")));
            
            // // Sends direct to known singleton proxy
            // clusterClient.Tell(new ClusterClient.Send("/user/manager", new ConfigureRoles(workerNodeConfig, "worker")));
            // clusterClient.Tell(new ClusterClient.Send("/user/manager", new ConfigureRoles(listenerNodeConfig, "listener")));

            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                Console.WriteLine("Stopping Cluster Bootstrapper...");
                await CoordinatedShutdown.Get(system).Run();
            };

            // Waits until service is terminated and then exits the program
            system.WhenTerminated.Wait();

            logger.Information("Cluster Bootstrapper stopped");
            Log.CloseAndFlush();
        }
    }
}