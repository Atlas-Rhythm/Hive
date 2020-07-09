using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hive.Permissions;
using Microsoft.AspNetCore.Mvc;

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

        private readonly Serilog.ILogger log;
        private readonly PermissionsManager<PermissionContext> permissions;

        public WeatherForecastController(Serilog.ILogger log, PermissionsManager<PermissionContext> perms)
        {
            this.log = log.ForContext<WeatherForecastController>();
            permissions = perms;
        }

        private static PermissionActionParseState action;

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            log.Debug("Hi there!");

            if (!permissions.CanDo("hive.weather.get", new PermissionContext(), ref action))
                throw new InvalidOperationException();

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
