using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hive.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;

namespace Hive
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var log = services.GetRequiredService<Serilog.ILogger>();
                try
                {
                    log.Debug("Configuring database");

                    var context = services.GetRequiredService<ModsContext>();
                    context.Database.EnsureCreated();

                    log.Debug("Database prepared");
                }
                catch (Exception e)
                {
                    log.Fatal(e, "An error ocurred while setting up the database");
                    Environment.Exit(1);
                    return;
                }
            }
                
            host.Run();
        }

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
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static LoggerConfiguration LibraryTypes(this LoggerDestructuringConfiguration conf)
            => conf.AsScalar<SemVer.Version>()
            .Destructure.AsScalar<SemVer.Range>();
    }
}
