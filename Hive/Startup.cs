using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hive.Controllers;
using Hive.Converters;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugins;
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
                    new PermissionsManager<PermissionContext>(sp.GetRequiredService<IRuleProvider>(), sp.GetService<Permissions.Logging.ILogger>(), "."));

            services.AddDbContext<ModsContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("Default"), 
                    o => o.UseNodaTime().SetPostgresVersion(12, 0)));

            services.AddAggregates();

            services.AddSingleton<IWeatherExtensions, WeatherThingImpl1>();
            services.AddSingleton<IWeatherExtensions, WeatherThingImpl2>();

            services.AddControllers();
        }

        private class WeatherThingImpl1 : IWeatherExtensions
        {
            public void DoThing()
            {
                throw new NotImplementedException();
            }

            public int GetThing1([TakesReturnValue] int a)
            {
                throw new NotImplementedException();
            }

            public int GetThing2([TakesReturnValue] int a, in int b)
            {
                throw new NotImplementedException();
            }

            public int GetThing3(int a, [ReturnLast] out int b)
            {
                throw new NotImplementedException();
            }

            public ref int GetThing4(int a, out int b)
            {
                throw new NotImplementedException();
            }

            public int GetThing5(int a)
            {
                throw new NotImplementedException();
            }
        }

        private class WeatherThingImpl2 : IWeatherExtensions
        {
            public void DoThing()
            {
                throw new NotImplementedException();
            }

            public int GetThing1([TakesReturnValue] int a)
            {
                throw new NotImplementedException();
            }

            public int GetThing2([TakesReturnValue] int a, in int b)
            {
                throw new NotImplementedException();
            }

            public int GetThing3(int a, [ReturnLast] out int b)
            {
                throw new NotImplementedException();
            }

            public ref int GetThing4(int a, out int b)
            {
                throw new NotImplementedException();
            }

            public int GetThing5(int a)
            {
                throw new NotImplementedException();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSerilogRequestLogging(options =>
            {

            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
