using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Hive.Plugins.Loading
{
    /// <summary>
    /// A plugin that has been loaded from disk.
    /// </summary>
    public class PluginInstance
    {
        /// <summary>
        /// Gets the main assembly of the plugin.
        /// </summary>
        public Assembly PluginAssembly { get; }
        /// <summary>
        /// Gets the <see cref="DirectoryInfo"/> of the plugin's directory.
        /// </summary>
        public DirectoryInfo PluginDirectory => LoadContext.PluginDirectory;
        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string Name => LoadContext.Name!;

        internal PluginLoadContext LoadContext { get; }
        internal DependencyContext DependencyContext { get; }

        internal PluginInstance(PluginLoadContext context)
        {
            LoadContext = context;
            PluginAssembly = context.LoadPlugin();
            DependencyContext = DependencyContext.Load(PluginAssembly);
        }
    }
}
