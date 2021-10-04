using System.Security.Cryptography;
using DryIoc;
using Hive.Controllers;
using Hive.Extensions;
using Hive.Graphing;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Plugins.Aggregates;
using Hive.Services;
using Hive.Services.Common;
using Hive.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using Serilog;

namespace Hive
{
    /// <summary>
    ///
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Startup constructor with configuration
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration) => Configuration = configuration;

        /// <summary>
        /// Configuration instance
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddDbContext<HiveContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("Default"),
                    o => o.UseNodaTime().SetPostgresVersion(12, 0)));

            _ = services.AddHiveGraphQL();

            var conditionalFeature = new HiveConditionalControllerFeatureProvider()
                .RegisterCondition<Auth0Controller>(Configuration.GetSection("Auth0").Exists());

            _ = services
                .AddControllers()
                .AddJsonOptions(opts => opts.JsonSerializerOptions.Converters.Add(ArbitraryAdditionalData.Converter))
                .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(conditionalFeature));
        }

        public void ConfigureContainer(IContainer container)
        {
            container.RegisterInstance<IClock>(SystemClock.Instance);
            container.Register<Permissions.Logging.ILogger, Logging.PermissionsProxy>();
            container.Register(Made.Of(() => new PermissionsManager<PermissionContext>(Arg.Of<IRuleProvider>(), Arg.Of<Permissions.Logging.ILogger>(), ".")), Reuse.Singleton);
            container.Register<SymmetricAlgorithm>(made: Made.Of(() => Rijndael.Create()));

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
            container.Register(typeof(IAggregate<>), typeof(Aggregate<>));

            // TODO: make this less repetitive and dumb
            container.Register<IChannelsControllerPlugin, HiveChannelsControllerPlugin>(Reuse.Singleton);
            container.Register<IGameVersionsPlugin, HiveGameVersionsControllerPlugin>(Reuse.Singleton);
            container.Register<IModsPlugin, HiveModsControllerPlugin>();
            container.Register<IResolveDependenciesPlugin, HiveResolveDependenciesControllerPlugin>(Reuse.Singleton);
            container.Register<IUploadPlugin, HiveDefaultUploadPlugin>(Reuse.Singleton);
            container.Register<IUserCreationPlugin, HiveUsernamePlugin>(Reuse.Singleton);
            container.Register<IUserPlugin, HiveUserPlugin>(Reuse.Singleton);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (Configuration.GetValue<bool>("RestrictEndpoints"))
            {
                _ = app.UseGuestRestrictionMiddleware();
            }

            if (env.IsDevelopment())
            {
                _ = app.UseDeveloperExceptionPage();
            }

            _ = app.UseExceptionHandlingMiddleware()
                .UseSerilogRequestLogging()
                .UseHttpsRedirection()
                .UseRouting()
                .UseAuthentication()
                .UseGraphQL<HiveSchema>("/graphql")
                .UseGraphQLAltair()
                .UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
