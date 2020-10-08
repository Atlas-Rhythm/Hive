using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GraphQL.Server;
using GraphQL.Types;
using Hive.Controllers;
using Hive.Converters;
using Hive.GraphQL;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace Hive
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddTransient<IRuleProvider, ConfigRuleProvider>()
                .AddTransient<Permissions.Logging.ILogger, Logging.PermissionsProxy>()
                .AddSingleton(sp =>
                    new PermissionsManager<PermissionContext>(sp.GetRequiredService<IRuleProvider>(), sp.GetService<Permissions.Logging.ILogger>(), "."))
                .AddSingleton<IChannelsControllerPlugin>(sp => new HiveChannelsControllerPlugin())
                .AddSingleton<IGameVersionsPlugin>(Span => new HiveGameVersionsControllerPlugin())
                //.AddSingleton<IProxyAuthenticationService>(sp => new VaulthAuthenticationService(sp.GetService<Serilog.ILogger>(), sp.GetService<IConfiguration>()));
                .AddSingleton<IProxyAuthenticationService>(sp => new MockAuthenticationService());

            services.AddDbContext<HiveContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("Default"),
                    o => o.UseNodaTime().SetPostgresVersion(12, 0)));

            services.AddAggregates();

            services.AddHttpContextAccessor();

            services.AddSingleton<HiveQuery>();
            services.AddSingleton<HiveSchema>();
            services.AddGraphQL((options, provider) =>
            {
                IWebHostEnvironment env = provider.GetRequiredService<IWebHostEnvironment>();
                Serilog.ILogger logger = provider.GetRequiredService<Serilog.ILogger>();

                options.EnableMetrics = env.IsDevelopment();
                options.UnhandledExceptionDelegate = ctx => logger.Error("An error has occured initializing GraphQL: {Message}", ctx.OriginalException.Message);
            }).AddSystemTextJson().AddGraphTypes(typeof(HiveSchema));

            services.AddControllers();
            services.AddAuthentication(a =>
            {
                a.AddScheme<MockAuthenticationHandler>("Bearer", "MockAuth");
                a.DefaultScheme = "Bearer";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Configure is required for Startup.")]
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseExceptionHandlingMiddleware();

            app.UseSerilogRequestLogging(options =>
            {
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            // See: https://developer.okta.com/blog/2019/04/16/graphql-api-with-aspnetcore
            // For adding GraphQL support in a reasonable way
            app.UseGraphQL<HiveSchema>("/graphql");
            app.UseGraphQLAltair();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}