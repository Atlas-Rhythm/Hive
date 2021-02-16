using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Hive.Plugins.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hive.Plugins.Loading
{
    internal class PluginLoader
    {
        private class LoaderConfig
        {
            public bool ImplicitlyLoadPlugins { get; set; } = true;
            public bool UsePluginSpecificConfig { get; set; } = false;
            public string PluginPath { get; set; } = "plugins";
            /// <summary>
            /// LoadPlugins specifies the names of the plugins to load when ImplicitlyLoadPlugins is false.
            /// </summary>
            public string[] LoadPlugins { get; set; } = Array.Empty<string>();
            /// <summary>
            /// ExcludePlugins specifies the names of the plugins to not load when it would otherwise load them.
            /// </summary>
            public string[] ExcludePlugins { get; set; } = Array.Empty<string>();
            /// <summary>
            /// This specifies the configuration objects to expose to each plugin, keyed on the name, for when UsePluginSpecificConfig is false.
            /// </summary>
            public Dictionary<string, IConfiguration> PluginConfigurations { get; set; } = new();
        }

        private readonly LoaderConfig config;
        private readonly PluginLoaderOptionsBuilder options;

        public PluginLoader(IConfigurationSection config, PluginLoaderOptionsBuilder options)
            => (this.config, this.options) = (config.Get<LoaderConfig>() ?? new(), options);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Caught exceptions are rethrown, just later on.")]
        public void LoadPlugins(IServiceCollection services, IHostEnvironment hostEnv)
        {
            var pluginDir = new DirectoryInfo(config.PluginPath);

            var pluginsToLoad = FindPluginsToLoad(pluginDir);

            var exceptions = new List<(Exception Exception, string PluginName)>();
            foreach (var plugin in pluginsToLoad)
            {
                try
                {
                    var plc = new PluginLoadContext(plugin);
                    var instance = new PluginInstance(plc);

                    InitPlugin(instance, services, hostEnv);
                    _ = services.AddSingleton(instance);
                }
                catch (AggregateException e)
                {
                    exceptions.AddRange(e.InnerExceptions.Select(e => (e, plugin.Name)));
                }
                catch (Exception e)
                {
                    exceptions.Add((e, plugin.Name));
                }
            }

            if (exceptions.Count > 0)
            {
                if (exceptions.Count == 1)
                {
                    var (exception, name) = exceptions.First();
                    throw new PluginLoadException(name, exception);
                }
                else
                {
                    throw new AggregateException(SR.PluginLoad_ErrorsWhenLoadingPlugins,
                        exceptions.Select(t => new PluginLoadException(t.PluginName, t.Exception)));
                }
            }
        }

        private IEnumerable<DirectoryInfo> FindPluginsToLoad(DirectoryInfo pluginDir)
            => FindPotentialPluginsToLoad(pluginDir)
                    .Where(d => !config.ExcludePlugins.Contains(d.Name));

        private IEnumerable<DirectoryInfo> FindPotentialPluginsToLoad(DirectoryInfo pluginDir)
        {
            if (!pluginDir.Exists)
                return Enumerable.Empty<DirectoryInfo>();

            if (config.ImplicitlyLoadPlugins)
            {
                return pluginDir.EnumerateDirectories();
            }
            else
            {
                return config.LoadPlugins
                    .SelectMany(name => pluginDir.EnumerateDirectories(name, SearchOption.TopDirectoryOnly));
            }
        }

        private void InitPlugin(PluginInstance plugin, IServiceCollection services, IHostEnvironment hostEnv)
        {
            // this will 1. locate the plugins' startup types, 2. construct them, and 3. call ConfigureServices on them.
            // it will *also* register an IStartupFilter instance which calls through to the Configure method.

            IConfiguration pluginConfig;
            if (config.UsePluginSpecificConfig)
            {
                var builder = new ConfigurationBuilder();
                options.ConfigurePluginConfigCb(builder, plugin);
                pluginConfig = builder.Build();
            }
            else
            {
                if (!config.PluginConfigurations.TryGetValue(plugin.Name, out var config2))
                {
                    config2 = new ConfigurationBuilder().Build();
                }

                pluginConfig = config2;
            }

            var types = plugin.PluginAssembly.SafeGetTypes();
            var pluginTypes = types.Where(t => t.GetCustomAttribute<PluginStartupAttribute>() is not null);

            var constructServices = new PluginServiceProvider(pluginConfig, plugin, hostEnv);

            foreach (var type in pluginTypes)
            {
                CreatePlugin(services, constructServices, type);
            }
        }

        private void CreatePlugin(IServiceCollection services, IServiceProvider constructServices, Type pluginType)
        {
            var instance = ActivatorUtilities.CreateInstance(constructServices, pluginType);
        }

        private sealed class PluginServiceProvider : IServiceProvider
        {
            private readonly IConfiguration configuration;
            private readonly PluginInstance instance;
            private readonly IHostEnvironment hostEnv;

            public PluginServiceProvider(IConfiguration config, PluginInstance plugin, IHostEnvironment hostEnv)
                => (configuration, instance, this.hostEnv) = (config, plugin, hostEnv);

            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(IConfiguration))
                {
                    return configuration;
                }
                if (serviceType == typeof(PluginInstance))
                {
                    return instance;
                }
                if (serviceType == typeof(DirectoryInfo))
                {
                    return instance.PluginDirectory;
                }
                if (serviceType == typeof(IHostEnvironment))
                {
                    return hostEnv;
                }
                return null;
            }
        }
    }
}
