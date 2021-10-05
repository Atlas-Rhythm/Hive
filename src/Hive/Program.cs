using DryIoc.Microsoft.DependencyInjection;
using Hive.Models;
using Hive.Plugins.Loading;
using Hive.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NodaTime;
using Serilog;
using Serilog.Configuration;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Version = Hive.Versioning.Version;

namespace Hive
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var log = services.GetRequiredService<ILogger>();

                // Make sure to run the registered plugin pre-configuration step
                var preConfigures = services.GetServices<PluginPreConfigureRegistration>();
                foreach (var prec in preConfigures)
                    await prec.Method(services).ConfigureAwait(false);

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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new DryIocServiceProviderFactory())
                .UseSerilog((host, services, logger) => logger
                    .ReadFrom.Configuration(host.Configuration)
                    .Destructure.LibraryTypes()
                    .Enrich.FromLogContext()
                    .Enrich.WithDemystifiedStackTraces()
                    .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
                        .WithDefaultDestructurers()
                        .WithDestructurers(new[] { new DbUpdateExceptionDestructurer() }))
                    .WriteTo.Console())
                .ConfigureWebHostDefaults(webBuilder => _ = webBuilder.UseStartup<Startup>())
                .UseWebHostPlugins(builder
                    => builder
                        .WithConfigurationKey("Plugins")
                        .WithApplicationConfigureRegistrar((sc, target, method)
                            => sc.AddSingleton<IStartupFilter>(sp => new CustomStartupFilter(sp, target, method)))
                        .WithPreConfigureRegistrar((sc, cb)
                            => sc.AddSingleton(new PluginPreConfigureRegistration(cb)))
                        .ConfigurePluginConfig((builder, plugin)
                            => builder
                                .AddJsonFile(Path.Combine(plugin.PluginDirectory.FullName, "pluginsettings.json"))
                                .AddEnvironmentVariables("PLUGIN_" + plugin.Name.Replace(".", "_", StringComparison.Ordinal) + "__"))
                        .OnPluginLoaded((services, plugin)
                            => GetApplicationPartManager(services).ApplicationParts.Add(new AssemblyPart(plugin.PluginAssembly)))
                );

        private static ApplicationPartManager GetApplicationPartManager(IServiceCollection services)
        {
            var manager = GetServiceInstanceFromCollection<ApplicationPartManager>(services);
            if (manager is null)
            {
                manager = new ApplicationPartManager();
                //   since PopulateDefaultParts is internal, we basically have to pray to God that this branch
                // never executes, or that if it does, not having the defaults doesn't break anything
                services.TryAddSingleton(manager);
            }
            return manager;
        }

        private static T? GetServiceInstanceFromCollection<T>(IServiceCollection services)
            => (T?)(services.LastOrDefault(d => d.ServiceType == typeof(T))?.ImplementationInstance);

        private class CustomStartupFilter : IStartupFilter
        {
            private readonly IServiceProvider services;
            private readonly object target;
            private readonly MethodInfo method;

            public CustomStartupFilter(IServiceProvider services, object target, MethodInfo method)
                => (this.services, this.target, this.method) = (services, target, method);

            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
                => app =>
                {
                    next(app);
                    services.InjectVoidMethod(method, t => t == typeof(IApplicationBuilder) ? app : null, null)(target);
                };
        }

        private record PluginPreConfigureRegistration(Func<IServiceProvider, Task> Method);

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

                var channel = new Channel { Name = "default" };
                _ = context.Channels.Add(channel);

                var gameVersion = new GameVersion { Name = "1.0.0", CreationTime = SystemClock.Instance.GetCurrentInstant() };
                _ = context.GameVersions.Add(gameVersion);

                var mod = new Mod
                {
                    ReadableID = "test-mod",
                    Version = new Version("0.1.0"),
                    UploadedAt = SystemClock.Instance.GetCurrentInstant(),
                    Uploader = new User { Username = "me" },
                    Channel = channel,
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
