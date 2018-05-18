using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Akka.Actor;
using Akka.Cluster.Routing;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Akka.Routing;
using AkkaLibrary.Actors;
using AkkaLibrary.Cluster.Actors;
using AkkaLibrary.Cluster.Configuration;
using AkkaLibrary.Common.Configuration;
using AkkaLibrary.Common.Utilities;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace ClusterTestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            var loggingSwitch = new LoggingLevelSwitch(LogEventLevel.Debug);
                        
            var elasticSearchOptions = new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
            {
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6,
                IndexFormat = "akka-serilog-index-{0:yyyy.MM.dd}",
                CustomFormatter = new ExceptionAsObjectJsonFormatter(renderMessage:true)
            };

            var defaultLogger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(loggingSwitch)
                .Enrich.WithProperty(LoggingExtensions.Identity, "System", true)
                .WriteTo.Console(outputTemplate: SerilogConfig.DefaultMessageTemplate, theme: SerilogConfig.DefaultConsoleTheme)
                // .WriteTo.Elasticsearch(elasticSearchOptions)
                .Filter.ByExcluding(x => x?.Properties["Identity"]?.ToString().Contains("System") ?? false)
                .CreateLogger();

            Log.Logger = defaultLogger;

            var configText = File.ReadAllText("hub.hocon");
            var clusterConfig = ConfigurationFactory.ParseString(configText);

            var sysCount = 3;

            var clusterConfigs = Enumerable.Range(4053, sysCount).Select(port =>
                clusterConfig.WithFallback(ConfigurationFactory.ParseString($"akka.remote.dot-netty.tcp.port = {port}, akka.cluster.roles = [hub]")));

            var systems = clusterConfigs.Select((cfg, i) => (name: $"System{i}", system: ActorSystem.Create("system", cfg)));

            //Setup
            var proxies = new List<IActorRef>();
            var gossipNodes = new List<IActorRef>();

            var count = 0;

            foreach (var sys in systems)
            {
                sys.system.ActorOf(
                    ClusterSingletonManager.Props(
                    Props.Create(() => new EchoActor()),
                    PoisonPill.Instance,
                    ClusterSingletonManagerSettings.Create(sys.system)
                ), "cluster-command");

                proxies.Add(sys.system.ActorOf(
                    ClusterSingletonProxy.Props(
                        "/user/cluster-command",
                        ClusterSingletonProxySettings.Create(sys.system).WithBufferSize(100)
                        ),
                    "cluster-command-proxy"
                ));

                var stateUpdater = new Func<HashSet<int>, HashSet<int>, HashSet<int>>((x, y) =>
                {
                    if (x == null)
                    {
                        return y;
                    }
                    if (y == null)
                    {
                        return x;
                    }
                    return x.Union(y).ToHashSet();
                });

                gossipNodes.Add(
                    sys.system.ActorOf(
                        Props.Create(() => new GossipActor<HashSet<int>>(
                            (count + 1).ToString(),
                            ActorRefs.Nobody,
                            stateUpdater
                            )), "gossip-node"));
                count++;
            }

            // Wait
            Thread.Sleep(1000);

            gossipNodes[0].Tell(new GossipActor<HashSet<int>>.NewState(new[] {1,2,3,4,5}.ToHashSet()));

            Thread.Sleep(5000);
            
            gossipNodes[2].Tell(new GossipActor<HashSet<int>>.NewState(new[] {10,20,30,40,50}.ToHashSet()));

            // Run
            foreach (var gossiper in gossipNodes)
            {
                gossiper.Tell(true);
            }

            Console.CancelKeyPress += async (s, a) =>
            {
                Console.WriteLine("Stopping Cluster...");

                foreach (var ga in gossipNodes)
                {
                    var response = ga.Ask<GossipActor<HashSet<int>>.StateRequestResponse>(new GossipActor<HashSet<int>>.StateRequest());
                    response.Wait();
                    defaultLogger
                        .ForContext(LoggingExtensions.Identity, "Output")
                        .Information("Final Data for actor:{Name} is {Items}", response.Result.Name, response.Result.State);
                }

                foreach (var sys in systems)
                {
                    await CoordinatedShutdown.Get(sys.system).Run();
                }
            };

            foreach (var sys in systems)
            {
                sys.system.WhenTerminated.Wait();
            }

            foreach (var sys in systems)
            {
                sys.system.Dispose();
            }

            Log.CloseAndFlush();
        }
    }
}