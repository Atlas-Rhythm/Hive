using AspNetCoreRateLimit;
using Hive.Models;
using Hive.Versioning;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using Serilog;
using Serilog.Configuration;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Version = Hive.Versioning.Version;

namespace Hive
{
    /// <summary>
    ///
    /// </summary>
    public static class Program
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="args"></param>
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var log = services.GetRequiredService<ILogger>();

                var ipPolicyStore = services.GetService<IIpPolicyStore>();
                if (ipPolicyStore != null) await ipPolicyStore.SeedAsync().ConfigureAwait(false);

                var clientPolicyStore = services.GetService<IClientPolicyStore>();
                if (clientPolicyStore != null) await clientPolicyStore.SeedAsync().ConfigureAwait(false);

                try
                {
                    log.Debug("Configuring database");

                    var context = services.GetRequiredService<HiveContext>();

                    _ = context.Database.EnsureCreated();

                    log.Debug("Database prepared");

                    DemoData(log, context, services);
                }
                catch (Exception e)
                {
                    log.Fatal(e, "An error ocurred while setting up the database");
                    throw;
                }
            }

            await host.RunAsync().ConfigureAwait(false);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((host, services, logger) => logger
                    .ReadFrom.Configuration(host.Configuration)
                    .Destructure.LibraryTypes()
                    .Enrich.FromLogContext()
                    .Enrich.WithDemystifiedStackTraces()
                    .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
                        .WithDefaultDestructurers()
                        .WithDestructurers(new[] { new DbUpdateExceptionDestructurer() }))
                    .WriteTo.Console())
                .ConfigureWebHostDefaults(webBuilder => _ = webBuilder.UseStartup<Startup>());

        private static LoggerConfiguration LibraryTypes(this LoggerDestructuringConfiguration conf)
            => conf.AsScalar<Version>()
            .Destructure.AsScalar<VersionRange>();

        [Conditional("DEBUG")]
        private static void DemoData(ILogger log, HiveContext context, IServiceProvider services)
        {
            if (context.Mods.Any()) return;

            log.Debug("Adding dummy data");

            using (var transaction = context.Database.BeginTransaction())
            {
                var emptyObject = JsonDocument.Parse("{}").RootElement.Clone();

                var channel = new Channel { Name = "default", AdditionalData = emptyObject };
                _ = context.Channels.Add(channel);

                var gameVersion = new GameVersion { Name = "1.0.0", AdditionalData = emptyObject, CreationTime = SystemClock.Instance.GetCurrentInstant() };
                _ = context.GameVersions.Add(gameVersion);

                var mod = new Mod
                {
                    ReadableID = "test-mod",
                    Version = new Version("0.1.0"),
                    UploadedAt = SystemClock.Instance.GetCurrentInstant(),
                    Uploader = new User { Username = "me" },
                    Channel = channel,
                    AdditionalData = emptyObject,
                    DownloadLink = new Uri("file:///"),
                };
                var loc = new LocalizedModInfo
                {
                    Language = "en-US",
                    Name = "Test Mod",
                    Description = "A mod in the DB for testing",
                    OwningMod = mod
                };

                var mr = new ModReference("dep-id", new VersionRange("^1.0.0"));

                mod.Dependencies.Add(mr);
                mod.AddGameVersion(gameVersion);

                _ = context.ModLocalizations.Add(loc);
                _ = context.Mods.Add(mod);
                _ = context.SaveChanges();

                transaction.Commit();
            }

            log.Debug("Dummy data added");
        }
    }
}
