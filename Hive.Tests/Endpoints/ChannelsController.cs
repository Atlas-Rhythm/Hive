﻿using Hive.Controllers;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugin;
using Hive.Services;
using Hive.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
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
            var plugin = new ChannelsControllerPlugin();
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
            var plugin = new Mock<ChannelsControllerPlugin>();
            plugin.Setup(m => m.GetChannelsAdditionalChecks(It.IsAny<User>())).Returns(false);
            plugin.Setup(m => m.GetChannelsFilter(It.IsAny<User>(), It.IsAny<IEnumerable<Channel>>()))
                .Returns((IEnumerable<Channel> c) => c);
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
            var plugin = new ChannelsControllerPlugin();
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
            Assert.NotNull(res.Result);
            var value = (res.Result as OkObjectResult).Value as IEnumerable<Channel>;
            Assert.NotNull(value);
            // Order of the channels isn't explicitly tested, just that the result contains the channels.
            foreach (var item in channelData)
                Assert.Contains(item, value);
        }

        [Fact]
        public async Task FilterPublic()
        {
            var plugin = new Mock<ChannelsControllerPlugin>();
            plugin.Setup(m => m.GetChannelsAdditionalChecks(It.IsAny<User>())).Returns(true);
            plugin.Setup(m => m.GetChannelsFilter(It.IsAny<User>(), It.IsAny<IEnumerable<Channel>>()))
                .Returns((IEnumerable<Channel> c) => c.Where(channel => channel.Name != "Beta"));
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
            Assert.NotNull(res.Result);
            var value = (res.Result as OkObjectResult).Value as IEnumerable<Channel>;
            Assert.NotNull(value);
            // Should only contain the first channel
            Assert.Contains(channelData.ElementAt(0), value);
            Assert.DoesNotContain(channelData.ElementAt(1), value);
        }

        public Mock<DbSet<Channel>> GetChannels(IQueryable<Channel> channels)
        {
            var channelSet = new Mock<DbSet<Channel>>();
            channelSet.As<IDbAsyncEnumerable<Channel>>()
                .Setup(m => m.GetAsyncEnumerator())
                .Returns(new TestDbAsyncEnumerator<Channel>(channels.GetEnumerator()));
            channelSet.As<IQueryable<Channel>>()
                .Setup(m => m.Provider)
                .Returns(new TestDbAsyncQueryProvider<Channel>(channels.Provider));
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

        private Controllers.ChannelsController CreateController(string permissionRule, ChannelsControllerPlugin plugin, IQueryable<Channel> channelData)
        {
            var logger = Serilog.Log.ForContext<ChannelsController>();
            var ruleProvider = MockRuleProvider();
            var hiveRule = new Rule("hive", "ctx.Hive | next(false)");
            var r = new Rule("hive.channel", permissionRule);
            ruleProvider.Setup(m => m.TryGetRule(hiveRule.Name, out hiveRule)).Returns(true);
            ruleProvider.Setup(m => m.TryGetRule(r.Name, out r)).Returns(true);
            var manager = new PermissionsManager<PermissionContext>(ruleProvider.Object);
            var perms = new PermissionsService(manager);

            var mockChannels = GetChannels(channelData);
            var mockContext = new Mock<HiveContext>();
            mockContext.Setup(m => m.Channels).Returns(mockChannels.Object);
            var channelsControllerPlugin = new Aggregation<ChannelsControllerPlugin>(plugin);
            var authService = new MockAuthenticationService();

            return new Controllers.ChannelsController(logger, perms, mockContext.Object, channelsControllerPlugin, authService);
        }
    }
}