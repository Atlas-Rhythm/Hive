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
    public class ModsController : TestDbWrapper
    {
        private readonly ITestOutputHelper helper;

        private static readonly IEnumerable<Channel> defaultChannels = new List<Channel>()
        {
            new Channel
            {
                Name = "Public",
                AdditionalData = DIHelper.EmptyAdditionalData
            },
            new Channel
            {
                Name = "Beta",
                AdditionalData = DIHelper.EmptyAdditionalData
            }
        };

        private static readonly IEnumerable<Mod> defaultMods = new List<Mod>()
        {
            GetPlaceholderMod("BSIPA", "Public"),
            GetPlaceholderMod("BSIPA", "Public", new Versioning.Version(0, 6, 9)),
            GetPlaceholderMod("SongCore", "Public"),
            GetPlaceholderMod("SiraUtil", "Public"),
            GetPlaceholderMod("ChromaToggle", "Beta"),
            GetPlaceholderMod("Counters+", "Beta"),
        };

        private static readonly IEnumerable<IModsPlugin> defaultPlugins = new List<IModsPlugin>()
        {
            new HiveModsControllerPlugin()
        };

        public ModsController(ITestOutputHelper helper) : base(new PartialContext
        {
            Channels = defaultChannels,
            Mods = defaultMods,
            ModLocalizations = defaultMods.SelectMany(m => m.Localizations)
        })
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
            Assert.Empty(value!.Where(x => x.ChannelName == "Beta"));
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
        public async Task SpecificModForbid()
        {
            var controller = CreateController("next(false)", defaultPlugins);
            var res = await controller.GetSpecificMod("BSIPA"); // We will look for BSIPA.

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<ForbidResult>(res.Result); // The above endpoint must fail due to the permission rule.
        }

        [Fact]
        public async Task SpecificModNonExistent()
        {
            var controller = CreateController("next(true)", defaultPlugins);
            var res = await controller.GetSpecificMod("william gay"); // Completely non-existent mod.

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<NotFoundObjectResult>(res.Result);  // The above endpoint must return 404.
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
        public async Task SpecificModLatestVersionStandard()
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
            // But! There's two versions of BSIPA, so we need to check that this version is REALLY the latest version.
            Assert.True(value?.Version == new Versioning.Version(1, 0, 0));
        }

        [Fact]
        public async Task SpecificModLatestVersionForbid()
        {
            var controller = CreateController("next(false)", defaultPlugins);
            var res = await controller.GetSpecificMod("BSIPA"); // We will look for BSIPA.

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<ForbidResult>(res.Result); // The above endpoint must fail due to the permission rule.
        }

        [Fact]
        public async Task SpecificModLatestVersionNonExistent()
        {
            var controller = CreateController("next(true)", defaultPlugins);
            var res = await controller.GetSpecificMod("william gay"); // Completely non-existent mod.

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<NotFoundObjectResult>(res.Result); // The above endpoint must return 404.
        }

        [Fact]
        public async Task SpecificModLatestVersionPluginFilter()
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

            res = await controller.GetSpecificMod("BSIPA"); // Next, we will look for SongCore, which is in Release.

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<OkObjectResult>(res.Result); // The above endpoint must succeed, since SongCore is public.
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as SerializedMod;
            Assert.NotNull(value); // We must be given a serialized mod back.

            // This mod must be SongCore.
            Assert.True(value?.ID == "BSIPA");
            // But! There's two versions of BSIPA, so we need to check that this version is REALLY the latest version.
            Assert.True(value?.Version == new Versioning.Version(1, 0, 0));
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

            // Serialize our request JSON data into a stream, which we will feed into our channel request.
            ModIdentifier identifier = new ModIdentifier
            {
                ID = "ChromaToggle",
                Version = "1.0.0"
            };

            using var stringStream = GenerateStreamFromString(JsonSerializer.Serialize(identifier));

            controller.ControllerContext.HttpContext = CreateMockRequest(stringStream);

            var res = await controller.MoveModToChannel("Public", identifier);

            Assert.NotNull(res); // Result must not be null.
            Assert.IsType<OkObjectResult>(res.Result); // The above endpoint must succeed.

            // While we can just call it a day right here, I would like to ensure that the changes
            // were properly made to the database. Thus, I will re-request the mod and confirm
            // the channel move procedure was reflected in the database.

            var confirmation = await controller.GetSpecificMod("ChromaToggle");

            Assert.NotNull(confirmation.Result);
            var confirmationResult = confirmation.Result as OkObjectResult;
            Assert.NotNull(confirmationResult);
            var confirmationMod = confirmationResult!.Value as SerializedMod;
            Assert.Equal("Public", confirmationMod!.ChannelName);
        }

        [Fact]
        public async Task MoveNonExistentMod()
        {
            var controller = CreateController("next(true)", defaultPlugins);

            // Serialize our request JSON data into a stream, which we will feed into our channel request.
            ModIdentifier identifier = new ModIdentifier
            {
                ID = "william gay",
                Version = "69.420.1337"
            };

            using var stringStream = GenerateStreamFromString(JsonSerializer.Serialize(identifier));

            controller.ControllerContext.HttpContext = CreateMockRequest(stringStream);

            var res = await controller.MoveModToChannel("Public", identifier);

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<NotFoundObjectResult>(res.Result); // The above endpoint must fail.
        }

        [Fact]
        public async Task MoveModToNonExistentChannel()
        {
            var controller = CreateController("next(true)", defaultPlugins);

            // Serialize our request JSON data into a stream, which we will feed into our channel request.
            ModIdentifier identifier = new ModIdentifier
            {
                ID = "Counters+",
                Version = "1.0.0"
            };

            using var stringStream = GenerateStreamFromString(JsonSerializer.Serialize(identifier));

            controller.ControllerContext.HttpContext = CreateMockRequest(stringStream);

            // Let's try moving this mod to a funny channel that doesn't exist
            var res = await controller.MoveModToChannel("sc2ad check your github notifications", identifier);

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<NotFoundObjectResult>(res.Result); // The above endpoint must fail.
        }

        [Fact]
        public async Task MoveModForbid()
        {
            var controller = CreateController("next(false)", defaultPlugins);

            // Serialize our request JSON data into a stream, which we will feed into our channel request.
            ModIdentifier identifier = new ModIdentifier
            {
                ID = "ChromaToggle",
                Version = "1.0.0"
            };

            using var stringStream = GenerateStreamFromString(JsonSerializer.Serialize(identifier));

            controller.ControllerContext.HttpContext = CreateMockRequest(stringStream);

            var res = await controller.MoveModToChannel("Public", identifier);

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<ForbidResult>(res.Result); // The above endpoint must fail due to the permission rule.
        }

        [Fact]
        public async Task MoveModUnauthorized()
        {
            var controller = CreateController("next(true)", defaultPlugins);

            // Try to request without specifying a user
            var res = await controller.MoveModToChannel("Public", null!);

            Assert.NotNull(res); // Result must not be null.
            Assert.IsType<UnauthorizedResult>(res.Result); // The above endpoint must fail since a user is not logged in/
        }

        private Controllers.ModsController CreateController(string permissionRule, IEnumerable<IModsPlugin> plugins)
        {
            var services = DIHelper.ConfigureServices(Options, helper, new ModsRuleProvider(permissionRule));

            services
                .AddTransient(sp => plugins)
                .AddScoped<Services.Common.ModService>()
                .AddScoped<Controllers.ModsController>()
                .AddAggregates();

            return services.BuildServiceProvider().GetRequiredService<Controllers.ModsController>();
        }

        // I need to set up a "proper" Mod object so that the controller won't throw a fit
        // from (understandably) having missing data.
        private static Mod GetPlaceholderMod(string name, string channel, Versioning.Version? version = null)
        {
            var mod = new Mod()
            {
                ReadableID = name,
                Version = version ?? new Versioning.Version(1, 0, 0),
                UploadedAt = new Instant(),
                EditedAt = null,
                Uploader = new User() { Username = "Billy bob joe" },
                Channel = defaultChannels.First(c => c.Name == channel),
                DownloadLink = new Uri("https://www.github.com/Atlas-Rhythm/Hive"),
                AdditionalData = DIHelper.EmptyAdditionalData
            };

            LocalizedModInfo info = new LocalizedModInfo()
            {
                OwningMod = mod,
                Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Name = name,
                Description = "if you read this, william gay"
            };

            mod.Localizations.Add(info);

            return mod;
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

        private static HttpContext CreateMockRequest(Stream body)
        {
            var requestMoq = new Mock<HttpRequest>();
            requestMoq.SetupGet(r => r.Body).Returns(body);
            requestMoq.SetupGet(r => r.Headers).Returns(new HeaderDictionary(
                new Dictionary<string, StringValues>()
                {
                    { HeaderNames.Authorization, new StringValues("Bearer: test") }
                })
            );

            var contextMoq = new Mock<HttpContext>();
            contextMoq.SetupGet(c => c.Request).Returns(requestMoq.Object);

            return contextMoq.Object;
        }
    }
}