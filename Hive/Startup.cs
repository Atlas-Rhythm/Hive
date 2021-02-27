using AspNetCoreRateLimit;
using System.Security.Cryptography;
using Hive.Controllers;
using Hive.Graphing;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugins;
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
using Serilog;

namespace Hive
{
    /// <summary>
    ///
    /// </summary>
    public class Startup
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration) => Configuration = configuration;

        /// <summary>
        ///
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            _ = services
                .AddSingleton<IClock>(SystemClock.Instance)
                .AddTransient<IRuleProvider>(sp =>
                    new ConfigRuleProvider(sp.GetRequiredService<ILogger>(), ".", Configuration.GetValue<string>("RuleSubfolder")))
                .AddTransient<Permissions.Logging.ILogger, Logging.PermissionsProxy>()
                .AddSingleton(sp =>
                    new PermissionsManager<PermissionContext>(sp.GetRequiredService<IRuleProvider>(), sp.GetService<Permissions.Logging.ILogger>(), "."))
                .AddSingleton<IChannelsControllerPlugin, HiveChannelsControllerPlugin>()
                .AddSingleton<IGameVersionsPlugin, HiveGameVersionsControllerPlugin>()
                .AddSingleton<IModsPlugin, HiveModsControllerPlugin>()
                .AddSingleton<IResolveDependenciesPlugin, HiveResolveDependenciesControllerPlugin>()
                .AddSingleton<IUploadPlugin, HiveDefaultUploadPlugin>()
                //.AddSingleton<IProxyAuthenticationService>(sp => new VaulthAuthenticationService(sp.GetService<Serilog.ILogger>(), sp.GetService<IConfiguration>()));
                //.AddSingleton<IProxyAuthenticationService, MockAuthenticationService>()
                .AddSingleton<IProxyAuthenticationService, Auth0AuthenticationService>()
                .AddSingleton<SymmetricAlgorithm>(sp => Rijndael.Create()); // TODO: pick an algo

            _ = services.AddDbContext<HiveContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("Default"),
                    o => o.UseNodaTime().SetPostgresVersion(12, 0)));

            _ = services.AddScoped<ModService>()
                .AddScoped<ChannelService>()
                .AddScoped<GameVersionService>()
                .AddScoped<DependencyResolverService>()
                .AddAggregates()
                .AddHiveQLTypes()
                .AddHiveGraphQL();

            if (Configuration.GetValue<bool>("UseRateLimiting"))
            {
                // AspNetCoreRateLimit requires this.
                // A PR was merged that made this unnecessary, but that has not made it into an official release.
                _ = services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

                _ = services.AddMemoryCache()
                    .Configure<ClientRateLimitOptions>(Configuration.GetSection("ClientRateLimiting"))
                    .Configure<ClientRateLimitPolicies>(Configuration.GetSection("ClientRateLimitPolicies"))
                    .Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"))
                    .Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"))
                    .AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>()
                    .AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>()
                    .AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>()
                    .AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            }

            _ = services.AddAuthentication(a =>
            {
                a.AddScheme<MockAuthenticationHandler>("Bearer", "MockAuth");
                a.DefaultScheme = "Bearer";
            });

            _ = services.AddControllers();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (Configuration.GetValue<bool>("UseRateLimiting"))
            {
                _ = app.UseClientRateLimiting()
                    .UseIpRateLimiting();
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
