using System;
using System.IO;
using AkkaLibrary.Common.Configuration;
using AkkaLibrary.Common.Logging;
using AkkaLibrary.Common.Utilities;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace AkkaLibrary.Cluster.Hub
{
    /// <summary>
    /// Creates and runs an empty actor system that serves as a seed node
    /// for a cluster.
    /// 
    /// Uses the "hub.hocon" as the base configuration but can take the
    /// IP address, port and system name as configurable paramters.
    /// 
    /// Creates a new .hocon file that shows the configuration as used
    /// by the system including modifications made to inject itself as
    /// a seed node
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var loggerFactory = new ElasticSearchLoggerFactory(new Uri("http://localhost:9200"));
            var consoleSwitch = loggerFactory.ConsoleSwitch;
            var elasticSwitch = loggerFactory.ElasticsearchSwitch;

            Log.Logger = loggerFactory.GetLogger();

            var logger = loggerFactory.GetLogger().ForContext(LoggingExtensions.Identity, "Hub", true);

            logger.Information("Cluster Hub started");

            HubService hubService = null;

            // If there are no arguments the just uses the hub.hocon file as it is.
            // Asks if the user is ok with that.
            if(args.Length == 0)
            {
                Console.WriteLine("No arguments specified. Should the Hub use the 'hub.hocon' file?");

                var shouldUse = GetResponse();
                if(shouldUse)
                {
                    Console.WriteLine("Using 'hub.hocon'...");
                    if(!File.Exists("hub.hocon"))
                    {
                        Console.WriteLine($"Could not find 'hub.hocon' in {Directory.GetCurrentDirectory()}");
                        return;
                    }
                    hubService = new HubService();
                }
                else
                {
                    Console.WriteLine("Stopping...");
                    Console.WriteLine("Usage: <Actor System Name> <IP Address> <port != 0>");
                    return;
                }
            }
            // No arguments or 3 arguments specifying the three configurables.
            else if(args.Length != 3)
            {
                Console.WriteLine($"Expected three arguments only. Received {string.Join(",", args)}");
                Console.WriteLine("Usage: <Actor System Name> <IP Address> <port != 0>");
                return;
            }
            else
            {
                var systemName = args[0];
                var ipAddress = args[1];
                if(!int.TryParse(args[2], out var portValue))
                {
                    Console.WriteLine("Could not parse the port {args[2]} as an integer");
                    return;
                }

                hubService = new HubService(ipAddress, portValue, systemName);
            }

            // Start the actor system
            if(!(hubService?.Start() ?? false))
            {
                Console.WriteLine("Failed to create actor system. Exiting.");
                return;
            }

            Console.WriteLine("Starting Cluster Hub.");
            Console.WriteLine("Press Ctrl+C to stop...");

            // Stop on escape sequence key press
            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                Log.Information("Stopping Hub Service...");
                await hubService.StopAsync();
            };

            // Waits until service is terminated and then exits the program
            hubService.TerminationHandle.Wait();
            Log.CloseAndFlush();
        }

        /// <summary>
        /// Helper function that gets a yes/no response from the user.
        /// </summary>
        /// <returns></returns>
        private static bool GetResponse()
        {
            bool confirmed = false;
            string key;
            const string yes = "yes";
            const string no = "no";
            do
            {
                Console.Write("Please enter [Y]es or [N]o and press [enter]:");
                key = Console.ReadLine().ToLowerInvariant();
                
                // Check yes
                if(yes.StartsWith(key))
                {
                    confirmed = true;
                    break;
                }
                else if(no.StartsWith(key))
                {
                    break;
                }
            } while (true);
            return confirmed;
        }
    }
}