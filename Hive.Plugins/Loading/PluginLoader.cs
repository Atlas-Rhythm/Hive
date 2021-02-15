using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Hive.Plugins.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Plugins.Loading
{
    internal class PluginLoader
    {
        private class LoaderConfig
        {
            public bool ImplicitlyLoadPlugins { get; set; } = true;
            public string PluginPath { get; set; } = "plugins";
            /// <summary>
            /// LoadPlugins specifies the names of the plugins to load when ImplicitlyLoadPlugins is false.
            /// </summary>
            public string[] LoadPlugins { get; set; } = Array.Empty<string>();
            /// <summary>
            /// ExcludePlugins specifies the names of the plugins to not load when it would otherwise load them.
            /// </summary>
            public string[] ExcludePlugins { get; set; } = Array.Empty<string>();
        }

        private readonly LoaderConfig config;

        public PluginLoader(IConfigurationSection config)
            => this.config = config.Get<LoaderConfig>() ?? new();

        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Caught exceptions are rethrown, just later on.")]
        public void LoadPlugins(IServiceCollection services)
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

                    InitPlugin(instance, services);
                    _ = services.AddSingleton(instance);
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

        private void InitPlugin(PluginInstance plugin, IServiceCollection services)
        {
            // TODO: implement

            // this will 1. locate the plugins' startup types, 2. construct them, and 3. call ConfigureServices on them.
            // it will *also* register an IStartupFilter instance which calls through to the Configure method.
        }
    }
}
