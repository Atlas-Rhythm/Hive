using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Hive.Plugins.Loading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hive.Plugins
{
    /// <summary>
    /// Indicates that the type this is attached to should be treated by the plugin loader as a startup type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Startup types must have one public constructor, which may accept either zero parameters, or any number of
    /// the following types:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Parameter Type</term>
    ///         <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///         <term><see cref="IConfiguration"/></term>
    ///         <description>The configuration of the plugin, wherever that resides, according to the application configuration.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="IHostEnvironment"/></term>
    ///         <description>The <see cref="IHostEnvironment"/> provided by the host.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="PluginInstance"/></term>
    ///         <description>The <see cref="PluginInstance"/> for the plugin being loaded. Useful to be able to easily find the plugin's
    ///         directory.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="DirectoryInfo"/></term>
    ///         <description>The <see cref="DirectoryInfo"/> for the plugin directory of the plugin being loaded. Useful to be able to
    ///         easily find the plugin's directory for loading assets.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// <para>
    /// Startup types may have one method named <c>ConfigureServices</c>, which may either take no parameters, or exactly
    /// one parameter of type <see cref="IServiceCollection"/>. This will be invoked to allow the plugin to add its services
    /// to dependency injection.
    /// </para>
    /// <para>
    /// Startup types may have one method named <c>Configure</c>, which may take any number of parameters which may be injected via the standard
    /// service provider, along with any additional services that the application may provide. For example, Hive itself provides
    /// <see cref="T:Microsoft.AspNetCore.Builders.IApplicationBuilder"/> to achieve parity with the standard ASP.NET Core Startup class.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    [SuppressMessage("Documentation", "CA1200:Avoid using cref tags with a prefix",
        Justification = "I reference a type that this project does not reference, because it is useful information.")]
    public sealed class PluginStartupAttribute : Attribute
    {
    }
}
