using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hive.Controllers;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugin;
using Hive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                .AddSingleton(sp => new PermissionsService(sp.GetRequiredService<PermissionsManager<PermissionContext>>()))
                .AddSingleton(sp => new ChannelsControllerPlugin())
                .AddSingleton<IAggregate<ChannelsControllerPlugin>>(sp => new Aggregation<ChannelsControllerPlugin>(sp.GetRequiredService<ChannelsControllerPlugin>()))
                //.AddSingleton<IProxyAuthenticationService>(sp => new VaulthAuthenticationService(sp.GetService<Serilog.ILogger>(), sp.GetService<IConfiguration>()));
                .AddSingleton<IProxyAuthenticationService>(Span => new MockAuthenticationService());

            services.AddDbContext<HiveContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("Default"),
                    o => o.UseNodaTime().SetPostgresVersion(12, 0)));

            services.AddControllers();
            services.AddAuthentication(a =>
            {
                a.AddScheme<MockAuthenticationHandler>("Bearer", "MockAuth");
                a.DefaultScheme = "Bearer";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}