using AkkaLibrary.Cluster.Actors;
using AkkaLibrary.Common.Interfaces;

namespace AkkaLibrary.Cluster.Interfaces
{
    /// <summary>
    /// Interface that defines a command sent to the <see cref="NodeConfigurator"/>
    /// </summary>
    public interface INodeCommand : IConfirmable<INodeCommand>
    {
    }
}