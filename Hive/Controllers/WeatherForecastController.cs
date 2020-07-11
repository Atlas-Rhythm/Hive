using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hive.Models;
using Hive.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hive.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ModsContext context;
        private readonly Serilog.ILogger log;
        private readonly PermissionsManager<PermissionContext> permissions;

        public WeatherForecastController(Serilog.ILogger log, PermissionsManager<PermissionContext> perms, ModsContext ctx)
        {
            this.log = log.ForContext<WeatherForecastController>();
            permissions = perms;
            context = ctx;
        }

        private static PermissionActionParseState action;

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            log.Debug("Hi there!");

            if (!permissions.CanDo("hive.weather.get", new PermissionContext(), ref action))
                throw new InvalidOperationException();

            var mod = context.Mods
                .Include(m => m.Localizations)
                .AsEnumerable()
                .Where(m => m.Localizations.Any(l => l.Language.Name == "en-US"))
                .Select(m => new { Mod = m, Info = m.Localizations.First(l => l.Language.Name == "en-US") })
                .ToList();

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
