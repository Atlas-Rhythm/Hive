using Hive.Plugins.Aggregates;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Plugins
{
    /// <summary>
    /// A static class containing extensions for <see cref="IServiceCollection"/> to support <see cref="IAggregate{T}"/>.
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Adds services for <see cref="IAggregate{T}"/> types.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> provided in <paramref name="services"/> for easy chaining.</returns>
        public static IServiceCollection AddAggregates(this IServiceCollection services)
            => services.AddSingleton(typeof(IAggregate<>), typeof(Aggregate<>));
    }
}
