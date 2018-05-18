using System;
using Akka.Actor;
using AkkaLibrary.Common.Interfaces;

namespace AkkaLibrary.Cluster.Configuration
{
    /// <summary>
    /// System configuration
    /// </summary>
    public class SystemConfiguration : IRoleConfiguration
    {
        public SystemConfiguration() { }

        public SystemConfiguration(params IPluginConfiguration[] configs)
        {
            Configs = configs;
        }

        /// <summary>
        /// A number of plugin configurations
        /// </summary>
        /// <returns></returns>
        public IPluginConfiguration[] Configs { get; set; }

        public Guid Id { get; } = Guid.NewGuid();
        
        #region Confirmation

        public IConfirmation<SystemConfiguration> GetConfirmation() => new SystemConfigurationConfirmation(Id);

        IConfirmation IConfirmable.GetConfirmation() => GetConfirmation();

        IConfirmation<IRoleConfiguration> IConfirmable<IRoleConfiguration>.GetConfirmation()
            => new SystemConfigurationConfirmation(Id);

        private class SystemConfigurationConfirmation : IConfirmation<SystemConfiguration>
        {
            public SystemConfigurationConfirmation(Guid guid)
            {
                ConfirmationId = guid;
            }

            public Guid ConfirmationId { get; }

            public string Description { get; } = nameof(SystemConfigurationConfirmation);
        }
        
        #endregion
    }
}