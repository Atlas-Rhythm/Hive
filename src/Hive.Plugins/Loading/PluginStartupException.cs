using System;
using System.Diagnostics.CodeAnalysis;
using Hive.Plugins.Resources;

namespace Hive.Plugins.Loading
{
    /// <summary>
    /// An exception that is thrown when a plugin fails during its startup.
    /// </summary>
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors",
        Justification = "The only missing constructor is the default one, and it never makes sense to not give this type a name.")]
    public class PluginStartupException : Exception
    {
        /// <summary>
        /// Gets the plugin type that failed.
        /// </summary>
        public Type PluginType { get; }

        /// <summary>
        /// Constructs a new <see cref="PluginStartupException"/> with the default message indicating a failure in the specified type.
        /// </summary>
        /// <param name="pluginType">The plugin type that failed during startup.</param>
        public PluginStartupException(Type pluginType)
            : base(SR.PluginLoadException_DefaultMessage.Format((pluginType ?? throw new ArgumentNullException(nameof(pluginType))).FullName))
        {
            PluginType = pluginType;
        }

        /// <summary>
        /// Constructs a new <see cref="PluginStartupException"/> with the specified message and plugin type.
        /// </summary>
        /// <param name="pluginType">The plugin type that failed during startup.</param>
        /// <param name="message">The message to construct the exception with.</param>
        public PluginStartupException(Type pluginType, string message) : base(message)
        {
            PluginType = pluginType;
        }

        /// <summary>
        /// Constructs a new <see cref="PluginStartupException"/> with the default message indicating a failure in the specified type, and
        /// the exception that caused the failure.
        /// </summary>
        /// <param name="pluginType">The plugin type that failed during startup.</param>
        /// <param name="innerException">The exception that caused the failure.</param>
        public PluginStartupException(Type pluginType, Exception innerException)
            : base(SR.PluginLoadException_DefaultMessage.Format((pluginType ?? throw new ArgumentNullException(nameof(pluginType))).FullName), innerException)
        {
            PluginType = pluginType;
        }

        /// <summary>
        /// Constructs a new <see cref="PluginStartupException"/> with the specified message, plugin type, and inner exception.
        /// </summary>
        /// <param name="pluginType">The plugin type that failed during startup.</param>
        /// <param name="message">The message to construct the exception with.</param>
        /// <param name="innerException">The exception that caused the failure.</param>
        public PluginStartupException(Type pluginType, string message, Exception innerException) : base(message, innerException)
        {
            PluginType = pluginType;
        }
    }
}
