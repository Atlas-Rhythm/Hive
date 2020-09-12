using Hive.Controllers;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugin;
using Hive.Services;
using Hive.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NodaTime;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Hive.Tests.Endpoints
{
    public class ChannelsController
    {
        [Fact]
        public async Task PermissionForbid()
        {
            var plugin = CreateDefaultPlugin();
            var channelData = new List<Channel>
            {
                new Channel { Name = "Public" },
                new Channel { Name = "Beta" }
            }.AsQueryable();

            var controller = CreateController("next(false)", plugin, channelData);
            var res = await controller.GetChannels();
            Assert.NotNull(res);
            // Should forbid based off of a permission failure.
            Assert.IsType<ForbidResult>(res.Result);
            Assert.NotNull(res.Result);
        }

        [Fact]
        public async Task PluginDeny()
        {
            var plugin = CreatePlugin();
            plugin.Setup(m => m.GetChannelsAdditionalChecks(It.IsAny<User>())).Returns(false);
            plugin.Setup(m => m.GetChannelsFilter(It.IsAny<User>(), It.IsAny<IEnumerable<Channel>>()))
                .Returns((User user, IEnumerable<Channel> c) => c);
            var channelData = new List<Channel>
            {
                new Channel { Name = "Public" },
                new Channel { Name = "Beta" }
            }.AsQueryable();

            var controller = CreateController("next(true)", plugin.Object, channelData);
            var res = await controller.GetChannels();
            Assert.NotNull(res);
            // Should forbid based off of a plugin failure.
            Assert.IsType<ForbidResult>(res.Result);
            Assert.NotNull(res.Result);
        }

        [Fact]
        public async Task Standard()
        {
            var plugin = CreateDefaultPlugin();
            var channelData = new List<Channel>
            {
                new Channel { Name = "Public" },
                new Channel { Name = "Beta" }
            }.AsQueryable();

            var controller = CreateController("next(true)", plugin, channelData);
            var res = await controller.GetChannels();
            Assert.NotNull(res);
            // Should succeed, return the correct channels.
            Assert.IsType<OkObjectResult>(res.Result);
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as IEnumerable<Channel>;
            Assert.NotNull(value);
            // Order of the channels isn't explicitly tested, just that the result contains the channels.
            foreach (var item in channelData)
                Assert.Contains(item, value);
        }

        [Fact]
        public async Task StandardFilter()
        {
            var plugin = CreateDefaultPlugin();
            var channelData = new List<Channel>
            {
                new Channel { Name = "Public" },
                new Channel { Name = "Beta" }
            }.AsQueryable();

            var controller = CreateController("isNull(ctx.Channel) | ctx.Channel.Name = \"Public\" | next(false)", plugin, channelData);
            var res = await controller.GetChannels();
            Assert.NotNull(res);
            // Should succeed, with only Public listed.
            Assert.IsType<OkObjectResult>(res.Result);
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as IEnumerable<Channel>;
            Assert.NotNull(value);
            // Should only contain the first channel
            Assert.Contains(channelData.ElementAt(0), value);
            Assert.DoesNotContain(channelData.ElementAt(1), value);
        }

        [Fact]
        public async Task PluginFilter()
        {
            var plugin = CreatePlugin();
            plugin.Setup(m => m.GetChannelsAdditionalChecks(It.IsAny<User>())).Returns(true);
            plugin.Setup(m => m.GetChannelsFilter(It.IsAny<User>(), It.IsAny<IEnumerable<Channel>>()))
                .Returns((User user, IEnumerable<Channel> c) => c.Where(channel => channel.Name != "Beta"));
            var channelData = new List<Channel>
            {
                new Channel { Name = "Public" },
                new Channel { Name = "Beta" }
            }.AsQueryable();

            var controller = CreateController("next(true)", plugin.Object, channelData);
            var res = await controller.GetChannels();
            Assert.NotNull(res);
            // Should succeed, with only Public listed.
            Assert.IsType<OkObjectResult>(res.Result);
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as IEnumerable<Channel>;
            Assert.NotNull(value);
            // Should only contain the first channel
            Assert.Contains(channelData.ElementAt(0), value);
            Assert.DoesNotContain(channelData.ElementAt(1), value);
        }

        public Mock<DbSet<Channel>> GetChannels(IQueryable<Channel> channels)
        {
            var channelSet = new Mock<DbSet<Channel>>();
            channelSet.As<IEnumerable<Channel>>()
                .Setup(m => m.GetEnumerator())
                .Returns(channels.GetEnumerator());
            channelSet.As<IQueryable<Channel>>()
                .Setup(m => m.Provider)
                .Returns(channels.Provider);
            channelSet.As<IQueryable<Channel>>().Setup(m => m.Expression).Returns(channels.Expression);
            channelSet.As<IQueryable<Channel>>().Setup(m => m.ElementType).Returns(channels.ElementType);
            channelSet.As<IQueryable<Channel>>().Setup(m => m.GetEnumerator()).Returns(channels.GetEnumerator());

            return channelSet;
        }

        private Mock<IRuleProvider> MockRuleProvider()
        {
            var mock = new Mock<IRuleProvider>();

            var start = SystemClock.Instance.GetCurrentInstant();
            mock.Setup(rules => rules.CurrentTime).Returns(() => SystemClock.Instance.GetCurrentInstant());
            mock.Setup(rules => rules.HasRuleChangedSince(It.IsAny<StringView>(), It.IsAny<Instant>())).Returns(false);
            mock.Setup(rules => rules.HasRuleChangedSince(It.IsAny<StringView>(), It.Is<Instant>(i => i < start))).Returns(true);
            mock.Setup(rules => rules.HasRuleChangedSince(It.IsAny<Rule>(), It.IsAny<Instant>())).Returns(false);
            mock.Setup(rules => rules.TryGetRule(It.IsAny<StringView>(), out It.Ref<Rule>.IsAny!)).Returns(false);
            return mock;
        }

        private Mock<IChannelsControllerPlugin> CreatePlugin() => new Mock<IChannelsControllerPlugin>();

        private IChannelsControllerPlugin CreateDefaultPlugin() => new HiveChannelsControllerPlugin();

        private Controllers.ChannelsController CreateController(string permissionRule, IChannelsControllerPlugin plugin, IQueryable<Channel> channelData)
        {
            var logger = new LoggerConfiguration().WriteTo.Debug().CreateLogger();
            var ruleProvider = MockRuleProvider();
            var hiveRule = new Rule("hive", "next(false)");
            var r = new Rule("hive.channel", permissionRule);
            ruleProvider.Setup(m => m.TryGetRule(hiveRule.Name, out hiveRule)).Returns(true);
            ruleProvider.Setup(m => m.TryGetRule(r.Name, out r)).Returns(true);
            var manager = new PermissionsManager<PermissionContext>(ruleProvider.Object, new List<(string, Delegate)>
            {
                ("isNull", new Func<object?, bool>(o => o is null))
            });

            var mockChannels = GetChannels(channelData);
            var mockContext = new Mock<HiveContext>();
            mockContext.Setup(m => m.Channels).Returns(mockChannels.Object);
            var channelsControllerPlugin = new Aggregation<IChannelsControllerPlugin>(plugin);
            var authService = new MockAuthenticationService();

            return new Controllers.ChannelsController(logger, manager, mockContext.Object, channelsControllerPlugin, authService);
        }
    }
}