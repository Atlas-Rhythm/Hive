using System;
using System.Diagnostics.CodeAnalysis;
using Hive.Plugins.Resources;

namespace Hive.Plugins.Loading
{
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors",
        Justification = "The only missing constructor is the default one, and it never makes sense to not give this type a name.")]
    public class PluginStartupException : Exception
    {
        public Type PluginType { get; }

        public PluginStartupException(Type pluginType)
            : base(SR.PluginLoadException_DefaultMessage.Format((pluginType ?? throw new ArgumentNullException(nameof(pluginType))).FullName))
        {
            PluginType = pluginType;
        }

        public PluginStartupException(Type pluginType, string message) : base(message)
        {
            PluginType = pluginType;
        }

        public PluginStartupException(Type pluginType, Exception innerException)
            : base(SR.PluginLoadException_DefaultMessage.Format((pluginType ?? throw new ArgumentNullException(nameof(pluginType))).FullName), innerException)
        {
            PluginType = pluginType;
        }

        public PluginStartupException(Type pluginType, string message, Exception innerException) : base(message, innerException)
        {
            PluginType = pluginType;
        }
    }
}
