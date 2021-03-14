using System;
using Microsoft.Extensions.Hosting;

namespace Hive.Plugins.Loading
{
    // TODO: figure out a way to get logging in here

    /// <summary>
    /// A set of extensions that expose plugin loading to <see cref="IHostBuilder"/>.
    /// </summary>
    public static class PluginHostBuilderExtensions
    {
        /// <summary>
        /// Enables plugin loading in the host.
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to enable plugin loading in.</param>
        /// <param name="configureOptions">A delegate which can be used to configure the plugin loader via <see cref="PluginLoaderOptionsBuilder"/>.</param>
        /// <returns>The builder passed in, to allow for easy method chaining.</returns>
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
                    var loader = new PluginLoader(config, builder);
                    loader.LoadPlugins(services, ctx.HostingEnvironment);
                });
        }
    }
}
