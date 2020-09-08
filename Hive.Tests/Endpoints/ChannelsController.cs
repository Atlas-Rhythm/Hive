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
            var logger = Serilog.Log.ForContext<ChannelsController>();
            var ruleProvider = MockRuleProvider();
            var r = new Rule("hive.channels", "false");
            ruleProvider.Setup(m => m.TryGetRule(r.Name, out r)).Returns(true);
            var manager = new PermissionsManager<PermissionContext>(ruleProvider.Object);
            var perms = new PermissionsService(manager);

            var channelData = new List<Channel>
            {
                new Channel { Name = "Public" },
                new Channel { Name = "Beta" }
            }.AsQueryable();

            var mockChannels = GetChannels(channelData);
            var mockContext = new Mock<HiveContext>();
            mockContext.Setup(m => m.Channels).Returns(mockChannels.Object);
            var channelsControllerPlugin = new Aggregation<ChannelsControllerPlugin>(new ChannelsControllerPlugin());
            var authService = new MockAuthenticationService();

            var controller = new Controllers.ChannelsController(logger, perms, mockContext.Object, channelsControllerPlugin, authService);
            var res = await controller.GetChannels();
            // Should forbid based off of a permission failure.
            Assert.IsType<ForbidResult>(res);
            Assert.NotNull(res);
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
    }
}