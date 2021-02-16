using System.IO;
using System.Reflection;

namespace Hive.Plugins.Loading
{
    public class PluginInstance
    {
        public Assembly PluginAssembly { get; }
        public DirectoryInfo PluginDirectory => LoadContext.PluginDirectory;
        public string Name => LoadContext.Name!;
        internal PluginLoadContext LoadContext { get; }

        internal PluginInstance(PluginLoadContext context)
        {
            LoadContext = context;
            PluginAssembly = context.LoadPlugin();
        }
    }
}
