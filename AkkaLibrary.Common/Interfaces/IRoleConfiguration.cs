using System;
using Akka.Actor;
using AkkaLibrary.Common.Interfaces;

namespace AkkaLibrary.Common.Interfaces
{
    /// <summary>
    /// Interface that describes a role configuration
    /// </summary>
    public interface IRoleConfiguration : IConfirmable<IRoleConfiguration>
    {
        /// <summary>
        /// Array of configurations that will be used to create
        /// the plugins present in the node
        /// </summary>
        /// <returns></returns>
        IPluginConfiguration[] Configs { get; }
    }
}