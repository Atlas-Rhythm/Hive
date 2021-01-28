using Hive.Models;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Services.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Hive.Tests.Endpoints
{
    public class ChannelsController : TestDbWrapper
    {
        private readonly ITestOutputHelper helper;

        private static readonly IEnumerable<Channel> defaultChannels = new List<Channel>
        {
            new Channel { Name = "Public", AdditionalData = DIHelper.EmptyAdditionalData },
            new Channel { Name = "Beta", AdditionalData =  DIHelper.EmptyAdditionalData }
        };

        public ChannelsController(ITestOutputHelper helper) : base(new PartialContext
        {
            Channels = defaultChannels
        })
        {
            this.helper = helper;
        }

        [Fact]
        public async Task PermissionForbid()
        {
            var plugin = CreateDefaultPlugin();

            var controller = CreateController("next(false)", plugin);
            var res = await controller.GetChannels();
            Assert.NotNull(res);
            // Should forbid based off of a permission failure.
            Assert.IsType<ForbidResult>(res.Result);
        }

        [Fact]
        public async Task PluginDeny()
        {
            var plugin = CreatePlugin();
            plugin.Setup(m => m.GetChannelsAdditionalChecks(It.IsAny<User>())).Returns(false);
            plugin.Setup(m => m.GetChannelsFilter(It.IsAny<User>(), It.IsAny<IEnumerable<Channel>>()))
                .Returns((User user, IEnumerable<Channel> c) => c);

            var controller = CreateController("next(true)", plugin.Object);
            var res = await controller.GetChannels();
            Assert.NotNull(res);
            // Should forbid based off of a plugin failure.
            Assert.NotNull(res.Result);
            Assert.IsType<ForbidResult>(res.Result);
        }

        [Fact]
        public async Task Standard()
        {
            var plugin = CreateDefaultPlugin();

            var controller = CreateController("next(true)", plugin);
            var res = await controller.GetChannels();
            Assert.NotNull(res);
            // Should succeed, return the correct channels.
            Assert.IsType<OkObjectResult>(res.Result);
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as IEnumerable<Channel>;
            Assert.NotNull(value);
            // Order of the channels isn't explicitly tested, just that the result contains the channels.
            foreach (var item in defaultChannels)
                Assert.Contains(item, value);
        }

        [Fact]
        public async Task StandardFilter()
        {
            var plugin = CreateDefaultPlugin();
            var controller = CreateController("isNull(ctx.Channel) | ctx.Channel.Name = \"Public\" | next(false)", plugin);
            var res = await controller.GetChannels();
            Assert.NotNull(res);
            // Should succeed, with only Public listed.
            Assert.IsType<OkObjectResult>(res.Result);
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as IEnumerable<Channel>;
            Assert.NotNull(value);
            // Should only contain the first channel
            Assert.Contains(defaultChannels.ElementAt(0), value);
            Assert.DoesNotContain(defaultChannels.ElementAt(1), value);
        }

        [Fact]
        public async Task PluginFilter()
        {
            var plugin = CreatePlugin();
            plugin.Setup(m => m.GetChannelsAdditionalChecks(It.IsAny<User>())).Returns(true);
            plugin.Setup(m => m.GetChannelsFilter(It.IsAny<User>(), It.IsAny<IEnumerable<Channel>>()))
                .Returns((User user, IEnumerable<Channel> c) => c.Where(channel => channel.Name != "Beta"));

            var controller = CreateController("next(true)", plugin.Object);
            var res = await controller.GetChannels();
            Assert.NotNull(res);
            // Should succeed, with only Public listed.
            Assert.IsType<OkObjectResult>(res.Result);
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as IEnumerable<Channel>;
            Assert.NotNull(value);
            // Should only contain the first channel
            Assert.Contains(defaultChannels.ElementAt(0), value);
            Assert.DoesNotContain(defaultChannels.ElementAt(1), value);
        }

        [Fact]
        public async Task CreateNewChannel()
        {
            var controller = CreateController("next(true)", CreateDefaultPlugin());
            var res = await controller.CreateNewChannel("archival");
            Assert.NotNull(res);
            // Should succeed, with only Public listed.
            Assert.IsType<OkObjectResult>(res.Result);
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as Channel;
            Assert.NotNull(value);
            Assert.True(value!.Name == "archival");
        }

        private static Mock<IChannelsControllerPlugin> CreatePlugin() => new();

        private static IChannelsControllerPlugin CreateDefaultPlugin() => new HiveChannelsControllerPlugin();

        private Controllers.ChannelsController CreateController(string permissionRule, IChannelsControllerPlugin plugin)
        {
            var ruleProvider = DIHelper.CreateRuleProvider();

            var hiveRule = new Rule("hive", "next(false)");
            var listChannels = new Rule("hive.channels.list", permissionRule);
            var filterChannels = new Rule("hive.channels.filter", permissionRule);
            var createChannel = new Rule("hive.channel.create", permissionRule);

            ruleProvider.Setup(m => m.TryGetRule(hiveRule.Name, out hiveRule)).Returns(true);
            ruleProvider.Setup(m => m.TryGetRule(listChannels.Name, out listChannels)).Returns(true);
            ruleProvider.Setup(m => m.TryGetRule(filterChannels.Name, out filterChannels)).Returns(true);
            ruleProvider.Setup(m => m.TryGetRule(createChannel.Name, out createChannel)).Returns(true);

            var services = DIHelper.ConfigureServices(
                Options,
                helper,
                ruleProvider.Object,
                new List<(string, Delegate)>
                {
                    ("isNull", new Func<object?, bool>(o => o is null))
                });

            services.AddSingleton<IChannelsControllerPlugin>(sp => new HiveChannelsControllerPlugin());
            services.AddSingleton(sp => plugin);
            services.AddAggregates();
            services.AddScoped<ChannelService>();
            services.AddScoped<Controllers.ChannelsController>();

            return services.BuildServiceProvider().GetRequiredService<Controllers.ChannelsController>();
        }
    }
}
