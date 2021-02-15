using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            public IEnumerable<string> LoadPlugins { get; set; } = Enumerable.Empty<string>();
            /// <summary>
            /// ExcludePlugins specifies the names of the plugins to not load when it would otherwise load them.
            /// </summary>
            public IEnumerable<string> ExcludePlugins { get; set; } = Enumerable.Empty<string>();
        }

        private readonly LoaderConfig config;

        public PluginLoader(IConfigurationSection config)
            => this.config = config.Get<LoaderConfig>() ?? new();

        public void LoadPlugins(IServiceCollection services)
        {
            var pluginDir = new DirectoryInfo(config.PluginPath);

            var pluginsToLoad = FindPluginsToLoad(pluginDir);

            foreach (var plugin in pluginsToLoad)
            {
                var plc = new PluginLoadContext(plugin);
                var instance = new PluginInstance(plc);

                if (TryInitPlugin(instance, services))
                {
                    // the initialization didn't fail horribly
                    _ = services.AddSingleton(instance);
                }
            }
        }

        private IEnumerable<DirectoryInfo> FindPluginsToLoad(DirectoryInfo pluginDir)
            => FindPotentialPluginsToLoad(pluginDir)
                    .Where(d => !config.ExcludePlugins.Contains(d.Name));

        private IEnumerable<DirectoryInfo> FindPotentialPluginsToLoad(DirectoryInfo pluginDir)
        {
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

        private bool TryInitPlugin(PluginInstance plugin, IServiceCollection services)
        {
            // ConfigureServices will be called here, so we need services
            // TODO: implement
            return false;
        }
    }
}
