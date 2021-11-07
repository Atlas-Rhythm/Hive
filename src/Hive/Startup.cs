using System;
using System.Security.Cryptography;
using System.Text.Json;
using DryIoc;
using Hive.Configuration;
using Hive.Controllers;
using Hive.Graphing;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugins.Aggregates;
using Hive.Services;
using Hive.Services.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Serilog;

namespace Hive
{
    internal class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        private void ConfigureConfiguration(IServiceCollection services)
        {
            if (Configuration.GetSection(Auth0Options.ConfigHeader).Exists())
            {
                _ = services.AddOptions<Auth0Options>()
                    .BindConfiguration(Auth0Options.ConfigHeader, a => a.BindNonPublicProperties = true)
                    .ValidateDataAnnotations();
            }
            _ = services.AddOptions<UploadOptions>()
                .BindConfiguration(UploadOptions.ConfigHeader, a => a.BindNonPublicProperties = true)
                .ValidateDataAnnotations();
            if (Configuration.GetSection(RestrictionOptions.ConfigHeader).Exists())
            {
                _ = services.AddOptions<RestrictionOptions>()
                    .BindConfiguration(RestrictionOptions.ConfigHeader)
                    .ValidateDataAnnotations();
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddDbContext<HiveContext>(options =>
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                    .UseNpgsql(Configuration.GetConnectionString("Default"),
                        o => o.UseNodaTime().SetPostgresVersion(12, 0)));

            _ = services.AddHiveGraphQL();

            var conditionalFeature = new HiveConditionalControllerFeatureProvider()
                .RegisterCondition<Auth0Controller>(Configuration.GetSection(Auth0Options.ConfigHeader).Exists());

            // Add config
            ConfigureConfiguration(services);

            _ = services
                .AddControllers()
                .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(conditionalFeature));
        }

        public void ConfigureContainer(IContainer container)
        {
            container.Register<ILogger>(Reuse.Transient,
                Made.Of(
                    r => ServiceInfo.Of<ILogger>(),
                    l => l.ForContext(Arg.Index<Type>(0)),
                    r => r.Parent.ImplementationType),
                setup: Setup.With(condition: r => r.Parent.ImplementationType is not null));

            container.RegisterInstance<IClock>(SystemClock.Instance);
            container.Register<JsonSerializerOptions>(Reuse.Singleton, made: Made.Of(() => ConstructHiveJsonSerializerOptions()));
            container.Register<Permissions.Logging.ILogger, Logging.PermissionsProxy>();
            container.Register(Made.Of(() => new PermissionsManager<PermissionContext>(Arg.Of<IRuleProvider>(), Arg.Of<Permissions.Logging.ILogger>(), ".")), Reuse.Singleton);
            container.Register<SymmetricAlgorithm>(Reuse.Singleton, made: Made.Of(() => Rijndael.Create()));

            if (Configuration.GetSection("Auth0").Exists())
            {
                container.RegisterMany<Auth0AuthenticationService>();
            }
            else if (container.Resolve<IHostEnvironment>().IsDevelopment())
            {
                // if Auth0 isn't configured, and we're in a dev environment, use
                container.RegisterMany<MockAuthenticationService>();
            }

            container.Register<IHttpContextAccessor, HttpContextAccessor>();
            container.Register<ModService>(Reuse.Scoped);
            container.Register<ChannelService>(Reuse.Scoped);
            container.Register<GameVersionService>(Reuse.Scoped);
            container.Register<DependencyResolverService>(Reuse.Scoped);
            container.Register(typeof(IAggregate<>), typeof(Aggregate<>), Reuse.Singleton);

            container.RegisterHiveGraphQL();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (Configuration.GetSection(RestrictionOptions.ConfigHeader).Exists())
            {
                _ = app.UseGuestRestrictionMiddleware();
            }

            if (env.IsDevelopment())
            {
                _ = app.UseDeveloperExceptionPage().UseExceptionHandlingMiddleware();
            }

            _ = app.UsePathBase(Configuration.GetValue<string>("PathBase"))
                .UseSerilogRequestLogging()
                .UseHttpsRedirection()
                .UseRouting()
                .UseAuthentication()
                .UseGraphQL<HiveSchema>("/graphql")
                .UseGraphQLAltair()
                .UseEndpoints(endpoints => endpoints.MapControllers());
        }

        private static JsonSerializerOptions ConstructHiveJsonSerializerOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                // We need to explicitly include fields for some ValueTuples to deserialize properly
                IncludeFields = true,
            }
            // Use Bcl time zone for Noda Time
            .ConfigureForNodaTime(DateTimeZoneProviders.Bcl);

            // Add AdditionalData converter
            options.Converters.Add(ArbitraryAdditionalData.Converter);
            // Add AdditionalData converter
            options.Converters.Add(NodaConverters.InstantConverter);

            return options;
        }
    }
}
