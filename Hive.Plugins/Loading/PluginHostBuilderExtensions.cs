using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Hive.Plugins.Loading
{
    // TODO: figure out a way to get logging in here
    public static class PluginHostBuilderExtensions
    {
        public static IHostBuilder UseWebHostPlugins(this IHostBuilder builder, string configurationKey = "PluginLoading")
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.ConfigureServices((ctx, services) =>
                {
                    var config = ctx.Configuration.GetSection(configurationKey);
                    var loader = new PluginLoader(config);
                    loader.LoadPlugins(services);
                });
        }
    }
}
