using System;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;

namespace AkkaLibrary.Cluster.Hub
{
    /// <summary>
    /// Service that houses a simple, empty actor system that serves as a
    /// seed node for a cluster.
    /// </summary>
    public class HubService
    {
        /// <summary>
        /// Default constructor that provides default parameters to the
        /// actor system factory.
        /// </summary>
        /// <returns></returns>
        public HubService() : this(null, null, null) { }

        /// <summary>
        /// Constructs a system with a specific hostname, port and system name
        /// supplied programatically.
        /// 
        /// All nodes in a cluster must have the same actor system name.
        /// </summary>
        /// <param name="hostname">The IP address or hostname</param>
        /// <param name="port">The non-zero port for the seed node</param>
        /// <param name="systemName">The non-empty system name</param>
        public HubService(string hostname, int? port, string systemName)
        {
            _hostname = hostname;
            _port = port;
            _systemName = systemName;
        }

        /// <summary>
        /// Termination task that can be waited on
        /// </summary>
        public Task TerminationHandle => _actorSystem.WhenTerminated;
        
        /// <summary>
        /// Creates the actor system with the specified parameters
        /// </summary>
        public bool Start()
        {
            try
            {
                _actorSystem = HubSystemFactory.Create(_systemName, _hostname, _port);
                return true;
            }
            catch(FileLoadException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Performs a coordinated shutdown of the system asynchronously.
        /// </summary>
        /// <returns>Shutdown task</returns>
        public async Task StopAsync()
        {
            await CoordinatedShutdown.Get(_actorSystem).Run();
        }

        private ActorSystem _actorSystem;
        private readonly string _hostname;
        private readonly int? _port;
        private readonly string _systemName;
    }
}