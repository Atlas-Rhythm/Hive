using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hive.Plugins.Loading
{
    // TODO: figure out a way to get logging in here
    public static class PluginHostBuilderExtensions
    {
        public static IHostBuilder UseWebHostPlugins(this IHostBuilder builder, Action<IServiceCollection, object, MethodInfo> registerStartupFilter, string configurationKey = "PluginLoading")
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));
            if (registerStartupFilter is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.ConfigureServices((ctx, services) =>
                {
                    var config = ctx.Configuration.GetSection(configurationKey);
                    var loader = new PluginLoader(config, registerStartupFilter);
                    loader.LoadPlugins(services);
                });
        }
    }
}
