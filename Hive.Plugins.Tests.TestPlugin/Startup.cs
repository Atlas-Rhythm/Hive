using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hive.Plugins.Tests.TestPlugin
{
    [PluginStartup]
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration config)
            => Configuration = config;

        public void ConfigureServices(IServiceCollection services)
        {
            _ = Configuration;
            _ = services;
        }

        // this doesn't need IApplicationBuilder because the tests don't provide it, giving notihg other than that which is injected
        public void Configure(IHost host)
        {
            _ = Configuration;
            _ = host;
        }
    }
}
