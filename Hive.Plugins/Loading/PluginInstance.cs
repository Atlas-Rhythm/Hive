using System.Reflection;

namespace Hive.Plugins.Loading
{
    public class PluginInstance
    {
        public Assembly PluginAssembly { get; }
        internal PluginLoadContext LoadContext { get; }

        internal PluginInstance(PluginLoadContext context)
        {
            LoadContext = context;
            PluginAssembly = context.LoadPlugin();
        }
    }
}
