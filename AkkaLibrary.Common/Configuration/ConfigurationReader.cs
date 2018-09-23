using System;
using System.Linq;
using Akka.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace AkkaLibrary.Configuration
{
    public class ConfigurationReader
    {
        private readonly string _filePath;
        private readonly IConfigurationRoot _config;

        public ConfigurationReader() : this("appsettings.json") { }

        public ConfigurationReader(string configurationFilePath)
        {
            _filePath = configurationFilePath;
            _config = new ConfigurationBuilder()
                        .AddJsonFile(_filePath)
                        .Build();
        }

        public Config GetAkkaHocon()
        {
            var cfg = _config.AsEnumerable();
            var dict = cfg.ToDictionary(x => x.Key.Replace(":", "."), x => x.Value?.Trim());

            return ConfigurationFactory.FromObject(dict);
        }
    }
}