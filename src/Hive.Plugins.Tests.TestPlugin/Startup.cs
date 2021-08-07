using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public void PreConfigure(IEnumerable<Action> preConfigCbs)
        {
            if (preConfigCbs is null)
                throw new ArgumentNullException(nameof(preConfigCbs));

            _ = Configuration;

            foreach (var preConfig in preConfigCbs)
            {
                preConfig();
            }
        }

        public async Task PreConfigureAsync(IEnumerable<Func<Task>> preConfigCbs)
        {
            if (preConfigCbs is null)
                throw new ArgumentNullException(nameof(preConfigCbs));

            _ = Configuration;

            foreach (var preConfig in preConfigCbs)
            {
                await preConfig().ConfigureAwait(false);
            }
        }

        // this doesn't need IApplicationBuilder because the tests don't provide it, giving nothing other than that which is injected
        public void Configure(IHost host, IEnumerable<Action<IConfiguration>> receiveConfig)
        {
            if (receiveConfig is null)
                throw new ArgumentNullException(nameof(receiveConfig));

            _ = Configuration;
            _ = host;

            foreach (var recv in receiveConfig)
            {
                recv(Configuration);
            }
        }
    }
}
