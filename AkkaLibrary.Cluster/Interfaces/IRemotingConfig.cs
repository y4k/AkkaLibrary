namespace AkkaLibrary.Cluster.Interfaces
{
    /// <summary>
    /// Interface for specifying remoting criteria in an Akka.NET <see cref="Config"/>
    /// </summary>
    public interface IRemotingConfig
    {
        /// <summary>
        /// The <see cref="IActorRefProvider"/> fully-qualified name that is used as the
        /// provider of actor references.
        /// 
        /// The will most likely be "remote"
        /// </summary>
        /// <returns>Fully-qualified classname or Akka.Net name</returns>
        string Provider { get; }

        /// <summary>
        /// The port to use for remoting
        /// </summary>
        /// <returns>Port number</returns>
        int Port { get; }
        
        /// <summary>
        /// The hostname of this system
        /// </summary>
        /// <returns>Hostname</returns>
        string Hostname { get; }
    }
}