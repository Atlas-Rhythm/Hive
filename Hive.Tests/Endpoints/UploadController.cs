using Hive.Controllers;
using Hive.Models;
using Hive.Models.Serialized;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Services;
using Hive.Utilities;
using Hive.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using NodaTime;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Version = Hive.Versioning.Version;

namespace Hive.Tests.Endpoints
{
    public class UploadController : TestDbWrapper
    {
        private readonly ITestOutputHelper helper;

        public UploadController(ITestOutputHelper helper)
            : base(new PartialContext
            {
                Channels = new[]
                {
                    new Channel
                    {
                        Name = "newly-uploaded",
                        AdditionalData = DIHelper.EmptyAdditionalData
                    }
                },
                GameVersions = new[]
                {
                    new GameVersion
                    {
                        Name = "1.12.1",
                        CreationTime = SystemClock.Instance.GetCurrentInstant(),
                        AdditionalData = DIHelper.EmptyAdditionalData
                    },
                    new GameVersion
                    {
                        Name = "1.13.0",
                        CreationTime = SystemClock.Instance.GetCurrentInstant(),
                        AdditionalData = DIHelper.EmptyAdditionalData
                    }
                },
            })
        {
            this.helper = helper;
        }

        [Fact]
        public void EnsureIUploadPluginValid()
        {
            var emptyPlugin = new HiveDefaultUploadPlugin();

            var aggregate = new Aggregate<IUploadPlugin>(new[] { emptyPlugin });

            _ = aggregate.Instance;
        }

        [Fact]
        public async Task ValidUploadProcess()
        {
            var controller = CreateController(new[] { new HiveDefaultUploadPlugin() }, "next(true)");

            controller.ControllerContext.HttpContext = CreateMockRequest(new MemoryStream());

            var result1 = await controller.Upload(new FormFile(new MemoryStream(), 0, 0, "Mod", "mod.zip"));

            Assert.NotNull(result1);

            Assert.Equal(Controllers.UploadController.ResultType.Confirm, result1.Value.Type);
            Assert.False(string.IsNullOrEmpty(result1.Value.ActionCookie));
            Assert.NotEqual(default, result1.Value.ExtractedData);
            Assert.NotNull(result1.Value.ExtractedData);

            var data = result1.Value.ExtractedData!;

            data = data with
            {
                ID = "test-mod-1",
                Version = new Version("0.0.1-a"),
                LocalizedModInfo = new SerializedLocalizedModInfo
                {
                    Language = "en-US",
                    Name = "Test Mod 1",
                    Description = "The first uploaded test mod"
                },
                Links = ImmutableList.Create(("project-home", "https://test-mod.bsmg.wiki/")),
                ChannelName = "newly-uploaded",
                SupportedGameVersions = ImmutableList.Create("1.12.1", "1.13.0"),
                Dependencies = ImmutableList.Create(new ModReference("bsipa", new VersionRange("^4.0.0"))),
            };

            var result2 = await controller.CompleteUpload(data, result1.Value.ActionCookie!);

            Assert.NotNull(result2);
            Assert.Null(result2.Result);
            Assert.Equal(Controllers.UploadController.ResultType.Success, result2.Value.Type);

            // TODO: sanely validate that the mod was in fact added to the DB
        }

        private Controllers.UploadController CreateController(IEnumerable<IUploadPlugin> plugins, string rule)
        {
            var services = DIHelper.ConfigureServices(Options, helper, new UploadsRuleProvider(rule));

            services
                .AddTransient(sp => plugins)
                .AddScoped<Controllers.UploadController>()
                .AddSingleton<ICdnProvider, MemoryTestCdn>()
                .AddSingleton<SymmetricAlgorithm>(sp => Rijndael.Create())
                .AddAggregates();

            return services.BuildServiceProvider().GetRequiredService<Controllers.UploadController>();
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
        private class UploadsRuleProvider : IRuleProvider
        {
            private readonly string permissionRule;

            public UploadsRuleProvider(string permissionRule)
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
                    case "hive.mods.upload":
                        gotten = new Rule(nameString, permissionRule);
                        return true;
                    default:
                        gotten = null;
                        return false;
                }
            }
        }

        private class MemoryTestCdn : ICdnProvider
        {
            private ulong nextId = 0;
            private readonly ConcurrentDictionary<string, (byte[] data, string name)> memoryStore = new();
            private readonly ConcurrentDictionary<string, Timer> expirationTimers = new();

            public async Task<CdnObject> UploadObject(string name, Stream data, Instant? expireAt)
            {
                var abw = new ArrayBufferWriter<byte>();
                int size;
                do
                {
                    var mem = abw.GetMemory();
                    size = await data.ReadAsync(mem);
                    abw.Advance(size);
                }
                while (size != 0);

                var barr = abw.WrittenSpan.ToArray();

                var objName = Interlocked.Increment(ref nextId).ToString();

                var obj = new CdnObject(objName);
                memoryStore.AddOrUpdate(objName, (barr, name), (_, _) => (barr, name));
                if (expireAt is not null)
                {
                    var delay = expireAt.Value - SystemClock.Instance.GetCurrentInstant();
                    var timer = new Timer(
                        _ => Task.Factory.StartNew(() => TryDeleteObject(obj), default, default, TaskScheduler.Default).Unwrap().Wait(), 
                        null, (int)delay.TotalMilliseconds, Timeout.Infinite);
                    expirationTimers.AddOrUpdate(objName, timer, (_, _) => timer);
                }

                return obj;
            }

            public Task<Uri> GetObjectActualUrl(CdnObject link)
            {
                if (!memoryStore.TryGetValue(link.UniqueId, out var tup))
                    return Task.FromException<Uri>(new InvalidOperationException());

                // this will never actually be used, so just throw in some nice info
                return Task.FromResult(new Uri($"object://{link.UniqueId}/?name={tup.name}&len={tup.data.Length}"));
            }

            public Task<string> GetObjectName(CdnObject link)
            {
                if (!memoryStore.TryGetValue(link.UniqueId, out var tup))
                    return Task.FromException<string>(new InvalidOperationException());

                return Task.FromResult(tup.name);
            }

            public async Task<bool> RemoveExpiry(CdnObject link)
            {
                if (!memoryStore.TryGetValue(link.UniqueId, out _))
                    return false;

                if (expirationTimers.TryRemove(link.UniqueId, out var timer))
                {
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                    await timer.DisposeAsync();
                }
                
                return true;
            }
            public Task SetExpiry(CdnObject link, Instant expireAt)
            {
                var delay = expireAt - SystemClock.Instance.GetCurrentInstant();
                var timer = new Timer(
                    _ => Task.Factory.StartNew(() => TryDeleteObject(link), default, default, TaskScheduler.Default).Unwrap().Wait(),
                    null, delay.Milliseconds, Timeout.Infinite);
                expirationTimers.AddOrUpdate(link.UniqueId, timer, (_, _) => timer);

                return Task.CompletedTask;
            }
            public async Task<bool> TryDeleteObject(CdnObject link)
            {
                if (expirationTimers.TryRemove(link.UniqueId, out var timer))
                {
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                    await timer.DisposeAsync();
                }

                return memoryStore.TryRemove(link.UniqueId, out _);
            }
        }
    }
}
