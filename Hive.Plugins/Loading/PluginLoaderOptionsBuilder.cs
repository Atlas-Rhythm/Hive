using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Plugins.Loading
{
    public class PluginLoaderOptionsBuilder
    {
        internal string ConfigurationKey = "PluginLoading";
        internal Action<IServiceCollection, object, MethodInfo> RegisterStartupFilter = (_, _, _) => { };
        internal Action<IConfigurationBuilder, PluginInstance> ConfigurePluginConfigCb = (_, _) => { };

        internal PluginLoaderOptionsBuilder() { }

        public PluginLoaderOptionsBuilder WithConfigurationKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            ConfigurationKey = key;
            return this;
        }

        public PluginLoaderOptionsBuilder WithApplicationConfigureRegistrar(Action<IServiceCollection, object, MethodInfo> registrar)
        {
            if (registrar is null)
                throw new ArgumentNullException(nameof(registrar));
            RegisterStartupFilter = registrar;
            return this;
        }

        public PluginLoaderOptionsBuilder ConfigurePluginConfig(Action<IConfigurationBuilder, PluginInstance> configure)
        {
            if (configure is null)
                throw new ArgumentNullException(nameof(configure));
            ConfigurePluginConfigCb = configure;
            return this;
        }
    }
}
