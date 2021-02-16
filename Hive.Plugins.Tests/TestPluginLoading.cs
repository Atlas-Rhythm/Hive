using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hive.Plugins.Loading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Hive.Plugins.Tests
{
    public class TestPluginLoading
    {
        private class StaticConfigurationSource : IConfigurationSource
        {
            public Dictionary<string, string> Values { get; set; } = new();

            public IConfigurationProvider Build(IConfigurationBuilder builder)
                => new StaticConfigurationProvider(Values);
        }

        private class StaticConfigurationProvider : ConfigurationProvider
        {
            public StaticConfigurationProvider(Dictionary<string, string> values)
                => Data = values;
        }

        private static IHostBuilder BuildLoadingHost(string cfgKey, Dictionary<string, string> cfgValues)
            => new HostBuilder()
                .ConfigureAppConfiguration(cfg
                    => cfg.Add(new StaticConfigurationSource
                    {
                        Values = cfgValues
                    }))
                .UseDefaultServiceProvider((ctx, spo) =>
                {

                })
                .UseWebHostPlugins((sc, target, method) => { }, cfgKey);

        [Fact]
        public void TestImplicitLoadPlugin()
        {
            var host = BuildLoadingHost("Plugins", new()
            {
                { "Plugins:PluginPath", "plugins" },
                { "Plugins:ImplicitlyLoadPlugins", "true" }
            })
                .Build();

            // TODO: verify that only the plugins that should have loaded did
        }

        [Fact]
        public void TestImplicitDenyLoadPlugin()
        {
            var host = BuildLoadingHost("Plugins", new()
            {
                { "Plugins:PluginPath", "plugins" },
                { "Plugins:ImplicitlyLoadPlugins", "true" },
                { "Plugins:ExcludePlugins:0", "Hive.Plugins.Test.TestPlugin" }
            })
                .Build();

            // TODO: verify that only the plugins that should have loaded did
        }

        [Fact]
        public void TestExplicitLoadPlugin()
        {
            var host = BuildLoadingHost("Plugins", new()
            {
                { "Plugins:PluginPath", "plugins" },
                { "Plugins:ImplicitlyLoadPlugins", "false" },
                { "Plugins:LoadPlugins:0", "Hive.Plugins.Test.TestPlugin" }
            })
                .Build();

            // TODO: verify that only the plugins that should have loaded did
        }

        [Fact]
        public void TestExplicitLoadExcludePlugin()
        {
            var host = BuildLoadingHost("Plugins", new()
            {
                { "Plugins:PluginPath", "plugins" },
                { "Plugins:ImplicitlyLoadPlugins", "false" },
                { "Plugins:LoadPlugins:0", "Hive.Plugins.Test.TestPlugin" },
                { "Plugins:ExcludePlugins:0", "Hive.Plugins.Test.TestPlugin" }
            })
                .Build();

            // TODO: verify that only the plugins that should have loaded did
        }

        [Fact]
        public void TestExplicitNoLoadPlugin()
        {
            var host = BuildLoadingHost("Plugins", new()
            {
                { "Plugins:PluginPath", "plugins" },
                { "Plugins:ImplicitlyLoadPlugins", "false" },
            })
                .Build();

            // TODO: verify that only the plugins that should have loaded did
        }
    }
}
