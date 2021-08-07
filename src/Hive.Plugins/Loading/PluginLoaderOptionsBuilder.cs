using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Plugins.Loading
{
    /// <summary>
    /// A type to allow for easy configuration of plugin loader settings.
    /// </summary>
    public class PluginLoaderOptionsBuilder
    {
        internal string ConfigurationKey = "PluginLoading";
        internal Action<IServiceCollection, object, MethodInfo> RegisterStartupFilter = (_, _, _) => { };
        internal Action<IServiceCollection, Func<IServiceProvider, Task>> RegisterPreConfigure = (_, _) => { };
        internal Action<IConfigurationBuilder, PluginInstance> ConfigurePluginConfigCb = (_, _) => { };
        internal Action<IServiceCollection, PluginInstance> OnPluginLoadedCb = (_, _) => { };

        internal PluginLoaderOptionsBuilder() { }

        /// <summary>
        /// Configures the configuration key to find plugin configuration in.
        /// </summary>
        /// <param name="key">The key to use.</param>
        /// <returns>The <see langword="this"/> object, to allow for easy method chaining.</returns>
        public PluginLoaderOptionsBuilder WithConfigurationKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            ConfigurationKey = key;
            return this;
        }

        /// <summary>
        /// Configures the callback to use to register plugins' <c>Configure</c> methods.
        /// </summary>
        /// <remarks>
        /// In an ASP.NET Core application, this callback will probably look something like this:
        /// <code lang="cs">
        /// (sc, target, method) =&gt; sc.AddSingleton&lt;IStartupFilter&gt;(sp =&gt; new CustomStartupFilter(sp, target, method)))
        /// </code>
        /// where <c>CustomStartupFilter</c> would, in the returned delegate, inject the method using the service provider using
        /// <see cref="LoaderUtils.InjectVoidMethod(IServiceProvider, MethodInfo, Func{Type, object?}, object?[])"/> either before
        /// or after calling the passed in delegate.
        /// </remarks>
        /// <param name="registrar">The delegate that will be used to register plugin <c>Configure</c> methods.</param>
        /// <returns>The <see langword="this"/> object, to allow for easy method chaining.</returns>
        public PluginLoaderOptionsBuilder WithApplicationConfigureRegistrar(Action<IServiceCollection, object, MethodInfo> registrar)
        {
            if (registrar is null)
                throw new ArgumentNullException(nameof(registrar));
            RegisterStartupFilter += registrar;
            return this;
        }

        /// <summary>
        /// Configures the callback to use to register plugins' <c>PreConfigure</c> and <c>PreConfigureAsync</c> methods.
        /// </summary>
        /// <remarks>
        /// This architecture assumes an ASP.NET Core-like application, where pre-configure callbacks would be called in the
        /// main application entry point, which is assumed to be <see langword="async"/>.
        /// </remarks>
        /// <param name="registrar">The delegate that will be used to register pre-configure methods.</param>
        /// <returns>The <see langword="this"/> object, to allow for easy method chaining.</returns>
        public PluginLoaderOptionsBuilder WithPreConfigureRegistrar(Action<IServiceCollection, Func<IServiceProvider, Task>> registrar)
        {
            if (registrar is null)
                throw new ArgumentNullException(nameof(registrar));
            RegisterPreConfigure += registrar;
            return this;
        }

        /// <summary>
        /// Configures the callback to use to configure plugin-specific config, if that is enabled.
        /// </summary>
        /// <param name="configure">The delegate to use to configure plugin-specific config.</param>
        /// <returns>The <see langword="this"/> object, to allow for easy method chaining.</returns>
        public PluginLoaderOptionsBuilder ConfigurePluginConfig(Action<IConfigurationBuilder, PluginInstance> configure)
        {
            if (configure is null)
                throw new ArgumentNullException(nameof(configure));
            ConfigurePluginConfigCb += configure;
            return this;
        }


        // This is needed to avoid referencing the target assembly for just a cref.
#pragma warning disable CA1200 // Avoid using cref tags with a prefix
        /// <summary>
        /// Configures a callback to invoke when a plugin is loaded. This may be used to, for example,
        /// register the plugin assembly as an <see cref="T:Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager"/>
        /// </summary>
        /// <param name="cb">The callback to call when a plugin is loaded.</param>
        /// <returns>The <see langword="this"/> object, to allow for easy method chaining.</returns>
#pragma warning restore CA1200 // Avoid using cref tags with a prefix
        public PluginLoaderOptionsBuilder OnPluginLoaded(Action<IServiceCollection, PluginInstance> cb)
        {
            if (cb is null)
                throw new ArgumentNullException(nameof(cb));
            OnPluginLoadedCb += cb;
            return this;
        }
    }
}
