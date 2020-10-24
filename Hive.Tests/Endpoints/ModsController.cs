using Hive.Controllers;
using Hive.Models;
using Hive.Models.Serialized;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Hive.Tests.Endpoints
{
    public class ModsController
    {
        private readonly ITestOutputHelper helper;

        private static IEnumerable<Mod> defaultMods = new List<Mod>()
        {
            GetPlaceholderMod("BSIPA", "Public"),
            GetPlaceholderMod("SongCore", "Public"),
            GetPlaceholderMod("SiraUtil", "Public"),
            GetPlaceholderMod("ChromaToggle", "Beta"),
            GetPlaceholderMod("Counters+", "Beta"),
        };

        private static IEnumerable<Channel> defaultChannels = new List<Channel>()
        {
            new Channel()
            {
                Name = "Public"
            },
            new Channel()
            {
                Name = "Beta"
            }
        };
        
        private static IEnumerable<IModsPlugin> defaultPlugins = new List<IModsPlugin>()
        {
            new HiveModsControllerPlugin()
        };

        public ModsController(ITestOutputHelper helper)
        {
            this.helper = helper;
        }

        [Fact]
        public async Task AllModsStandard()
        {
            var controller = CreateController("next(true)", defaultPlugins);
            var res = await controller.GetAllMods();

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<OkObjectResult>(res.Result); // The above endpoint must succeed.
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as IEnumerable<SerializedMod>;
            Assert.NotNull(value); // We must be given a list of mods back.

            // Check if the result given back contains all of the mods we put into it.
            // Pretty weird LINQ statement, but it checks that all mods has a serialized mod with the same name.
            Assert.True(defaultMods.All(x => value?.Any(y => x.ReadableID == y.ID) ?? false));
        }

        [Fact]
        public async Task SpecificModStandard()
        {
            var controller = CreateController("next(true)", defaultPlugins);
            var res = await controller.GetSpecificMod("BSIPA"); // We will look for BSIPA.

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<OkObjectResult>(res.Result); // The above endpoint must succeed.
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as SerializedMod;
            Assert.NotNull(value); // We must be given a serialized mod back.

            // This mod must be BSIPA.
            Assert.True(value?.ID == "BSIPA");
        }

        [Fact]
        public async Task GetNonExistentMod()
        {
            var controller = CreateController("next(true)", defaultPlugins);
            var res = await controller.GetSpecificMod("william gay"); // Completely non-existent mod.

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<NotFoundResult>(res.Result); // The above endpoint must return 404.
        }

        [Fact]
        public async Task AllModsPluginFilter()
        {
            var controller = CreateController("next(true)", new List<IModsPlugin>()
            {
                new HiveModsControllerPlugin(),
                new BetaModsFilterPlugin() // Filters all mods on Beta channel
            });
            var res = await controller.GetAllMods();

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<OkObjectResult>(res.Result); // The above endpoint must succeed.
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as IEnumerable<SerializedMod>;
            Assert.NotNull(value); // We must be given a list of mods back.

            // Ensure that no mods belong to the beta channel.
            Assert.False(value?.Any(x => x.ChannelName == "Beta"));
        }

        [Fact]
        public async Task SpecificModPluginFilter()
        {
            var controller = CreateController("next(true)", new List<IModsPlugin>()
            {
                new HiveModsControllerPlugin(),
                new BetaModsFilterPlugin() // Filters all mods on Beta channel
            });
            var res = await controller.GetSpecificMod("Counters+"); // We will look for Counters+, which is in Beta.

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<ForbidResult>(res.Result); // The above endpoint must be fail, since Counters+ is in Beta.

            res = await controller.GetSpecificMod("SongCore"); // Next, we will look for SongCore, which is in Release.

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<OkObjectResult>(res.Result); // The above endpoint must succeed, since SongCore is public.
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as SerializedMod;
            Assert.NotNull(value); // We must be given a serialized mod back.

            // This mod must be SongCore.
            Assert.True(value?.ID == "SongCore");
        }

        [Fact]
        public async Task AllModsForbid()
        {
            var controller = CreateController("next(false)", defaultPlugins); // By default, no one is allowed access.
            var res = await controller.GetAllMods(); // Send the request

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<ForbidResult>(res.Result); // The above endpoint must be fail due to the permission rule.
        }

        [Fact]
        public async Task MoveModStandard()
        {
            var controller = CreateController("next(true)", defaultPlugins);
            var getMod = await controller.GetSpecificMod("ChromaToggle"); // Grab the mod we want to move.

            // Serialize our mod into JSON, since we need to re-attach it with the request.
            Assert.NotNull(getMod.Result);
            var getModResult = getMod.Result as OkObjectResult;
            Assert.NotNull(getModResult);
            var serializedMod = getModResult!.Value as SerializedMod;

            // Serialize our mod into JSON, since we need to re-attach it with the request.
            var json = JsonSerializer.Serialize(serializedMod);

            helper.WriteLine(json);

            using var stringStream = GenerateStreamFromString(json);

            // Because this endpoint reads from the request body, we need to Moq a HttpContext
            var requestMoq = new Mock<HttpRequest>();
            requestMoq.SetupGet(r => r.Body).Returns(stringStream);
            requestMoq.SetupGet(r => r.Headers).Returns(new HeaderDictionary(
                new Dictionary<string, StringValues>()
                {
                    { HeaderNames.Authorization, new StringValues("Bearer: test") }
                })
            );

            var contextMoq = new Mock<HttpContext>();
            contextMoq.SetupGet(c => c.Request).Returns(requestMoq.Object);

            var actionContext = new ActionContext(contextMoq.Object, new RouteData(), new ControllerActionDescriptor());

            controller.ControllerContext.HttpContext = contextMoq.Object;

            var res = await controller.MoveModToChannel("Public");

            Assert.NotNull(res); // Result must not be null.
            Assert.IsType<OkObjectResult>(res); // The above endpoint must succeed.

            var confirmation = await controller.GetSpecificMod("ChromaToggle"); // Re-grab our mod to confirm its new home.

            Assert.NotNull(confirmation.Result);
            var confirmationResult = getMod.Result as OkObjectResult;
            Assert.NotNull(confirmationResult);
            var confirmationMod = confirmationResult!.Value as SerializedMod;
            Assert.Equal("Public", confirmationMod!.ChannelName);
        }

        private Controllers.ModsController CreateController(string permissionRule, IEnumerable<IModsPlugin> plugins)
        {
            var services = DIHelper.ConfigureServices(
                helper,
                new ModsRuleProvider(permissionRule),
                null,
                new HiveContext()
                {
                    Mods = GetDBSetFromQueryable(defaultMods.AsQueryable()).Object,
                    Channels = GetDBSetFromQueryable(defaultChannels.AsQueryable()).Object
                });

            services
                .AddTransient(sp => plugins)
                .AddScoped<Controllers.ModsController>()
                .AddAggregates();

            return services.BuildServiceProvider().GetRequiredService<Controllers.ModsController>();
        }

        // Taken from sc2ad's test for ChannelsControllers
        // TODO: Move to helper type
        private static Mock<DbSet<T>> GetDBSetFromQueryable<T>(IQueryable<T> versions) where T : class
        {
            var channelSet = new Mock<DbSet<T>>();
            channelSet.As<IEnumerable<T>>()
                .Setup(m => m.GetEnumerator())
                .Returns(versions.GetEnumerator());
            channelSet.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(versions.Provider);
            channelSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(versions.Expression);
            channelSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(versions.ElementType);
            channelSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(versions.GetEnumerator());

            return channelSet;
        }

        // I need to set up a "proper" Mod object so that the controller won't throw a fit
        // from (understandably) having missing data.
        private static Mod GetPlaceholderMod(string name, string channel)
        {
            var localization = new List<LocalizedModInfo>()
            {
                new LocalizedModInfo()
                { 
                    Language = CultureInfo.CurrentCulture,
                    Name = name,
                    Description = "Hi danike, this shit not null"
                }
            };
            return new Mod()
            {
                ReadableID = name,
                Version = new Versioning.Version(1, 0, 0),
                UploadedAt = new Instant(),
                EditedAt = null,
                Uploader = new User() { Username = "Billy bob joe" },
                Channel = new Channel() { Name = channel },
                DownloadLink = new Uri("https://www.github.com/AtlasRhythm/Hive"),
                Localizations = localization,
                AdditionalData = JsonDocument.Parse("{}").RootElement.Clone()
            };
        }

        // This plugin will filter out a mod if it's in the beta channel. Super super basic but works.
        private class BetaModsFilterPlugin : IModsPlugin
        {
            public bool GetSpecificModAdditionalChecks(User? user, Mod contextMod) => contextMod.Channel.Name != "Beta";
        }

        // This is taken from GameVersionsController to have a configurable permission rule.
        private class ModsRuleProvider : IRuleProvider
        {
            private readonly string permissionRule;

            public ModsRuleProvider(string permissionRule)
            {
                this.permissionRule = permissionRule;
            }

            public bool HasRuleChangedSince(StringView name, Instant time) => true;

            public bool HasRuleChangedSince(Rule rule, Instant time) => true;

            public bool TryGetRule(StringView name, [MaybeNullWhen(false)] out Rule gotten)
            {
                string nameString = name.ToString();
                switch (nameString)
                {
                    case "hive":
                        gotten = new Rule(nameString, "next(false)");
                        return true;
                    case "hive.mod":
                        gotten = new Rule(nameString, permissionRule);
                        return true;
                    default:
                        gotten = null;
                        return false;
                }
            }
        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
