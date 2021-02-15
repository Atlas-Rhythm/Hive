using System;
using System.Diagnostics.CodeAnalysis;
using Hive.Plugins.Resources;

namespace Hive.Plugins.Loading
{
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors",
        Justification = "The only missing constructor is the default one, and it never makes sense to not give this type a name.")]
    public class PluginLoadException : Exception
    {
        public string PluginName { get; }

        public PluginLoadException(string pluginName) : base(SR.PluginLoadException_DefaultMessage.Format(pluginName))
        {
            PluginName = pluginName;
        }
        public PluginLoadException(string pluginName, string message) : base(message)
        {
            PluginName = pluginName;
        }

        public PluginLoadException(string pluginName, Exception innerException) : base(SR.PluginLoadException_DefaultMessage.Format(pluginName), innerException)
        {
            PluginName = pluginName;
        }
        public PluginLoadException(string pluginName, string message, Exception innerException) : base(message, innerException)
        {
            PluginName = pluginName;
        }
    }
}
