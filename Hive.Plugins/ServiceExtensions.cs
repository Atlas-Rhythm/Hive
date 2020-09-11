using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hive.Plugins
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddAggregates(this IServiceCollection services)
            => services.AddSingleton(typeof(IAggregate<>), typeof(Aggregate<>));
    }
}
