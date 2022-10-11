using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DryIoc;
using Hive.Models;
using Hive.Models.Serialized;
using Hive.Services.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Hive.Tests.Endpoints
{
    // TODO: Replace all Assert.True with Assert.Equal when applicable
    public class ModsController : TestDbWrapper
    {
        private readonly ITestOutputHelper helper;

        private static readonly IEnumerable<Channel> defaultChannels = new List<Channel>()
        {
            new Channel
            {
                Name = "Public"
            },
            new Channel
            {
                Name = "Beta"
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
                new ModTestHelper.BetaModsFilterPlugin() // Filters all mods on Beta channel
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

            TestHelpers.AssertNotNull(res); // Result must not be null.
            TestHelpers.AssertNotNull(res.Result);
            TestHelpers.AssertForbid(res.Result); // The above endpoint must fail due to the permission rule.
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
                new ModTestHelper.BetaModsFilterPlugin() // Filters all mods on Beta channel
            });
            var res = await controller.GetSpecificMod("Counters+"); // We will look for Counters+, which is in Beta.

            TestHelpers.AssertNotNull(res); // Result must not be null.
            TestHelpers.AssertNotNull(res.Result);
            TestHelpers.AssertForbid(res.Result); // The above endpoint must be fail, since Counters+ is in Beta.

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
        public async Task SpecificModSpecificVersionStandard()
        {
            var controller = CreateController("next(true)", defaultPlugins);
            var res = await controller.GetSpecificModSpecificVersion("BSIPA", "0.6.9"); // We will look for BSIPA.

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<OkObjectResult>(res.Result); // The above endpoint must succeed.
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as SerializedMod;
            Assert.NotNull(value); // We must be given a serialized mod back.

            // This mod must be BSIPA.
            Assert.Equal("BSIPA", value.ID);
            // But! There's two versions of BSIPA, so we need to check that this version is REALLY 0.6.9.
            Assert.Equal(new Versioning.Version(0, 6, 9), value.Version);
        }

        [Fact]
        public async Task SpecificModSpecificVersionForbid()
        {
            var controller = CreateController("next(false)", defaultPlugins);
            var res = await controller.GetSpecificModSpecificVersion("BSIPA", "0.6.9"); // We will look for BSIPA.


            TestHelpers.AssertNotNull(res); // Result must not be null.
            TestHelpers.AssertNotNull(res.Result);
            TestHelpers.AssertForbid(res.Result); // The above endpoint must fail due to the permission rule.
        }

        [Fact]
        public async Task SpecificModSpecificVersionInvalidVersion()
        {
            var controller = CreateController("next(true)", defaultPlugins);
            var res = await controller.GetSpecificModSpecificVersion("BSIPA", "abcdef"); // Completely non-existent mod.

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<BadRequestObjectResult>(res.Result); // The above endpoint must return 404.
        }

        [Fact]
        public async Task SpecificModSpecificVersionNonExistent()
        {
            var controller = CreateController("next(true)", defaultPlugins);
            var res = await controller.GetSpecificModSpecificVersion("william gay", "1.0.0"); // Completely non-existent mod.

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<NotFoundObjectResult>(res.Result); // The above endpoint must return 404.
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

            TestHelpers.AssertNotNull(res); // Result must not be null.
            TestHelpers.AssertNotNull(res.Result);
            TestHelpers.AssertForbid(res.Result); // The above endpoint must fail due to the permission rule.
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
                new ModTestHelper.BetaModsFilterPlugin() // Filters all mods on Beta channel
            });
            var res = await controller.GetSpecificMod("Counters+"); // We will look for Counters+, which is in Beta.

            TestHelpers.AssertNotNull(res); // Result must not be null.
            TestHelpers.AssertNotNull(res.Result);
            TestHelpers.AssertForbid(res.Result); // The above endpoint must be fail, since Counters+ is in Beta.

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
        public async Task MoveModStandard()
        {
            var controller = CreateController("next(true)", defaultPlugins);
            var getMod = await controller.GetSpecificMod("ChromaToggle"); // Grab the mod we want to move.

            // Serialize our mod into JSON, since we need to re-attach it with the request.
            Assert.NotNull(getMod.Result);
            var getModResult = getMod.Result as OkObjectResult;
            Assert.NotNull(getModResult);

            // Serialize our request JSON data into a stream, which we will feed into our channel request.
            var identifier = new ModIdentifier
            {
                ID = "ChromaToggle",
                Version = "1.0.0"
            };

            using var stringStream = TestHelpers.GenerateStreamFromString(JsonSerializer.Serialize(identifier));

            controller.ControllerContext.HttpContext = TestHelpers.CreateMockRequest(stringStream);

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
            var identifier = new ModIdentifier
            {
                ID = "william gay",
                Version = "69.420.1337"
            };

            using var stringStream = TestHelpers.GenerateStreamFromString(JsonSerializer.Serialize(identifier));

            controller.ControllerContext.HttpContext = TestHelpers.CreateMockRequest(stringStream);

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
            var identifier = new ModIdentifier
            {
                ID = "Counters+",
                Version = "1.0.0"
            };

            using var stringStream = TestHelpers.GenerateStreamFromString(JsonSerializer.Serialize(identifier));

            controller.ControllerContext.HttpContext = TestHelpers.CreateMockRequest(stringStream);

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
            var identifier = new ModIdentifier
            {
                ID = "ChromaToggle",
                Version = "1.0.0"
            };

            using var stringStream = TestHelpers.GenerateStreamFromString(JsonSerializer.Serialize(identifier));

            controller.ControllerContext.HttpContext = TestHelpers.CreateMockRequest(stringStream);

            var res = await controller.MoveModToChannel("Public", identifier);

            TestHelpers.AssertNotNull(res); // Result must not be null.
            TestHelpers.AssertNotNull(res.Result);
            TestHelpers.AssertForbid(res.Result); // The above endpoint must fail due to the permission rule.
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
            var container = DIHelper.ConfigureServices(Options, _ => { }, helper, new ModTestHelper.ModsRuleProvider(permissionRule));

            container.RegisterInstance(plugins);
            container.Register<ModService>(Reuse.Scoped);
            container.Register<Controllers.ModsController>(Reuse.Scoped);

            var scope = container.CreateScope();

            var controller = scope.ServiceProvider.GetRequiredService<Controllers.ModsController>();

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        // I need to set up a "proper" Mod object so that the controller won't throw a fit
        // from (understandably) having missing data.
        private static User DummyUser = null!;
        private static Mod GetPlaceholderMod(string name, string channel, Versioning.Version? version = null)
        {
            // We need this because GetPlaceholderMod is called before DummyUser is initialized
            DummyUser ??= new() { Username = "Billy bob joe", AlternativeId = "bbj altid" };
            var mod = new Mod()
            {
                ReadableID = name,
                Version = version ?? new Versioning.Version(1, 0, 0),
                UploadedAt = new Instant(),
                EditedAt = null,
                Uploader = DummyUser,
                Channel = defaultChannels.First(c => c.Name == channel),
                DownloadLink = new Uri("https://www.github.com/Atlas-Rhythm/Hive")
            };

            var info = new LocalizedModInfo()
            {
                OwningMod = mod,
                Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Name = name,
                Description = "if you read this, william gay"
            };

            //mod.Localizations.Add(info);

            return mod;
        }
    }
}
