using System;
using System.Diagnostics.CodeAnalysis;
using Hive.Plugins.Resources;

namespace Hive.Plugins.Loading
{
    /// <summary>
    /// An exception that is thrown when a plugin fails to load.
    /// </summary>
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors",
        Justification = "The only missing constructor is the default one, and it never makes sense to not give this type a name.")]
    public class PluginLoadException : Exception
    {
        /// <summary>
        /// Gets the name of the plugin that failed to load.
        /// </summary>
        public string PluginName { get; }

        /// <summary>
        /// Constructs a new <see cref="PluginLoadException"/> with the default message with a given plugin name.
        /// </summary>
        /// <param name="pluginName">The name of the plugin that failed to load.</param>
        public PluginLoadException(string pluginName) : base(SR.PluginLoadException_DefaultMessage.Format(pluginName))
        {
            PluginName = pluginName;
        }
        /// <summary>
        /// Constructs a new <see cref="PluginLoadException"/> with the specified plugin name and message.
        /// </summary>
        /// <param name="pluginName">The name fo the plugin that failed to load.</param>
        /// <param name="message">The message to construct the exception with.</param>
        public PluginLoadException(string pluginName, string message) : base(message)
        {
            PluginName = pluginName;
        }
        /// <summary>
        /// Constructs a new <see cref="PluginLoadException"/> with the default message, a given plugin name, and given inner exception.
        /// </summary>
        /// <param name="pluginName">The name of the plugin that failed to load.</param>
        /// <param name="innerException">The exception that caused the failure.</param>
        public PluginLoadException(string pluginName, Exception innerException) : base(SR.PluginLoadException_DefaultMessage.Format(pluginName), innerException)
        {
            PluginName = pluginName;
        }
        /// <summary>
        /// Constructs a new <see cref="PluginLoadException"/> with the specified plugin name, message, and inner exception.
        /// </summary>
        /// <param name="pluginName">The name of the plugin that failed to load.</param>
        /// <param name="message">The message to construct the exception with.</param>
        /// <param name="innerException">The exception that caused the failure.</param>
        public PluginLoadException(string pluginName, string message, Exception innerException) : base(message, innerException)
        {
            PluginName = pluginName;
        }
    }
}
