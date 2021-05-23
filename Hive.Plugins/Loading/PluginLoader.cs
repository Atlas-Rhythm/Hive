using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Hive.Plugins.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hive.Plugins.Loading
{
    internal class PluginLoader
    {
        private class LoaderConfig
        {
            public bool ImplicitlyLoadPlugins { get; set; } = true;
            public bool UsePluginSpecificConfig { get; set; }
            public string PluginPath { get; set; } = "plugins";
            /// <summary>
            /// LoadPlugins specifies the names of the plugins to load when ImplicitlyLoadPlugins is false.
            /// </summary>
            public string[] LoadPlugins { get; set; } = Array.Empty<string>();
            /// <summary>
            /// ExcludePlugins specifies the names of the plugins to not load when it would otherwise load them.
            /// </summary>
            public string[] ExcludePlugins { get; set; } = Array.Empty<string>();
            // Unfortunately, we can't deserialize to a dictionary of sections, because that would make too much sense
            /*
            /// <summary>
            /// This specifies the configuration objects to expose to each plugin, keyed on the name, for when UsePluginSpecificConfig is false.
            /// </summary>
            public Dictionary<string, ConfigurationSection> PluginConfigurations { get; set; } = new();
            */
        }

        private readonly LoaderConfig config;
        private readonly IConfigurationSection sharedConfigSection;
        private readonly PluginLoaderOptionsBuilder options;

        public PluginLoader(IConfigurationSection config, PluginLoaderOptionsBuilder options)
        {
            this.config = config.Get<LoaderConfig>() ?? new();
            sharedConfigSection = config.GetSection("PluginConfigurations");
            this.options = options;
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Caught exceptions are rethrown, just later on.")]
        public void LoadPlugins(IServiceCollection services, IHostEnvironment hostEnv)
        {
            var pluginDir = new DirectoryInfo(config.PluginPath);

            var pluginsToLoad = FindPluginsToLoad(pluginDir);

            var exceptions = new List<(Exception Exception, string PluginName)>();
            foreach (var plugin in pluginsToLoad)
            {
                try
                {
                    var plc = new PluginLoadContext(plugin);
                    var instance = new PluginInstance(plc);

                    InitPlugin(instance, services, hostEnv);
                    _ = services.AddSingleton(instance);
                }
                catch (AggregateException e)
                {
                    exceptions.AddRange(e.InnerExceptions.Select(e => (e, plugin.Name)));
                }
                catch (Exception e)
                {
                    exceptions.Add((e, plugin.Name));
                }
            }

            if (exceptions.Count > 0)
            {
                if (exceptions.Count == 1)
                {
                    var (exception, name) = exceptions.First();
                    throw new PluginLoadException(name, exception);
                }
                else
                {
                    throw new AggregateException(SR.PluginLoad_ErrorsWhenLoadingPlugins,
                        exceptions.Select(t => new PluginLoadException(t.PluginName, t.Exception)));
                }
            }
        }

        private IEnumerable<DirectoryInfo> FindPluginsToLoad(DirectoryInfo pluginDir)
            => FindPotentialPluginsToLoad(pluginDir)
                    .Where(d => !config.ExcludePlugins.Contains(d.Name));

        private IEnumerable<DirectoryInfo> FindPotentialPluginsToLoad(DirectoryInfo pluginDir)
        {
            if (!pluginDir.Exists)
                return Enumerable.Empty<DirectoryInfo>();

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

        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Caught exceptions are rethrown, just later on.")]
        private void InitPlugin(PluginInstance plugin, IServiceCollection services, IHostEnvironment hostEnv)
        {
            // this will 1. locate the plugins' startup types, 2. construct them, and 3. call ConfigureServices on them.
            // it will *also* register an IStartupFilter instance which calls through to the Configure method.

            IConfiguration pluginConfig;
            if (config.UsePluginSpecificConfig)
            {
                var builder = new ConfigurationBuilder();
                options.ConfigurePluginConfigCb(builder, plugin);
                pluginConfig = builder.Build();
            }
            else
            {
                pluginConfig = sharedConfigSection.GetSection(plugin.Name);
            }

            var types = plugin.PluginAssembly.SafeGetTypes();
            var pluginTypes = types.Where(t => t.GetCustomAttribute<PluginStartupAttribute>() is not null);

            var constructServices = new PluginServiceProvider(pluginConfig, plugin, hostEnv);

            var exceptions = new List<(Exception Exception, Type Type)>();
            foreach (var type in pluginTypes)
            {
                try
                {
                    var instance = ActivatorUtilities.CreateInstance(constructServices, type);

                    var configureServices = FindMethod(type, "ConfigureServices");
                    ConfigureWith(services, configureServices, instance);

                    var configure = FindMethod(type, "Configure");
                    if (configure is not null)
                    {
                        options.RegisterStartupFilter(services, instance, configure);
                    }

                    // finally, register the instance to the service collection so that it can be injected
                    _ = services.AddSingleton(type, instance);
                }
                catch (Exception e)
                {
                    exceptions.Add((e, type));
                }
            }

            if (exceptions.Count > 0)
            {
                if (exceptions.Count == 1)
                {
                    var (exception, type) = exceptions.First();
                    throw new PluginStartupException(type, exception);
                }
                else
                {
                    throw new AggregateException(exceptions.Select(t => new PluginStartupException(t.Type, t.Exception)));
                }
            }
        }

        private static MethodInfo? FindMethod(Type targetType, string methodName)
        {
            var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.Name == methodName).ToList();
            if (methods.Count > 1)
            {
                throw new InvalidOperationException(SR.PluginLoad_MultipleOverloadsNotSupported.Format(methodName));
            }
            return methods.FirstOrDefault();
        }

        private static void ConfigureWith(IServiceCollection services, MethodInfo? configureServices, object instance)
        {
            if (configureServices is null)
                return;

            var parameters = configureServices.GetParameters();
            if (parameters.Length > 1 || parameters.Any(p => p.ParameterType != typeof(IServiceCollection)))
            {
                throw new InvalidOperationException(SR.PluginLoad_ConfigureServicesCanOnlyTakeServiceCollection);
            }

            var args = new object[parameters.Length];
            if (parameters.Length != 0)
            {
                args[0] = services;
            }
            _ = configureServices.InvokeWithoutWrappingExceptions(instance, args);
            return;
        }

        private sealed class PluginServiceProvider : IServiceProvider
        {
            private readonly IConfiguration configuration;
            private readonly PluginInstance instance;
            private readonly IHostEnvironment hostEnv;

            public PluginServiceProvider(IConfiguration config, PluginInstance plugin, IHostEnvironment hostEnv)
                => (configuration, instance, this.hostEnv) = (config, plugin, hostEnv);

            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(IConfiguration))
                {
                    return configuration;
                }
                if (serviceType == typeof(PluginInstance))
                {
                    return instance;
                }
                if (serviceType == typeof(DirectoryInfo))
                {
                    return instance.PluginDirectory;
                }
                if (serviceType == typeof(IHostEnvironment))
                {
                    return hostEnv;
                }
                return null;
            }
        }
    }
}
