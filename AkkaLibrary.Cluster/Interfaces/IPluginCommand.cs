using AkkaLibrary.Common.Interfaces;

namespace AkkaLibrary.Cluster.Configuration
{
    /// <summary>
    /// Interface that defines a command sent to a plugin
    /// </summary>
    public interface IPluginCommand : IConfirmable<IPluginCommand>
    {
        /// <summary>
        /// Name of the plugin
        /// </summary>
        /// <returns></returns>
        string PluginName { get; }
    }
}