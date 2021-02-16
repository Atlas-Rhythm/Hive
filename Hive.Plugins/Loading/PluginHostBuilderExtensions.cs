using System;
using System.IO;
using Microsoft.Extensions.Hosting;

namespace Hive.Plugins.Loading
{
    // TODO: figure out a way to get logging in here
    public static class PluginHostBuilderExtensions
    {
        public static IHostBuilder UseWebHostPlugins(this IHostBuilder builder, Action<PluginLoaderOptionsBuilder> configureOptions)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));
            if (configureOptions is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.ConfigureServices((ctx, services) =>
                {
                    var builder = new PluginLoaderOptionsBuilder();
                    configureOptions(builder);

                    var config = ctx.Configuration.GetSection(builder.ConfigurationKey);
                    var loader = new PluginLoader(config, builder.RegisterStartupFilter, builder.ConfigurePluginConfigCb);
                    loader.LoadPlugins(services, ctx.HostingEnvironment);
                });
        }
    }
}
