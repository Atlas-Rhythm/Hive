using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hive.Plugins.Tests
{
    public static class DIHelper
    {
        /// <summary>
        /// Returns a created <see cref="IAggregate{T}"/> from the given plugins.
        /// </summary>
        /// <typeparam name="T">The plugin type to use.</typeparam>
        /// <param name="items">The items to use when aggregating.</param>
        /// <returns>The created aggregation of <paramref name="items"/>.</returns>
        public static IAggregate<T> Create<T>(params T[] items) where T : class
        {
            var services = new ServiceCollection();
            foreach (var item in items)
            {
                services.AddSingleton((sp) => item);
            }
            services.AddAggregates();
            return services.BuildServiceProvider().GetRequiredService<IAggregate<T>>();
        }
    }
}