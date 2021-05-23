using Microsoft.Extensions.DependencyInjection;

namespace Hive.Extensions
{
    /// <summary>
    /// Extensions for <see cref="IServiceCollection"/>
    /// </summary>
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds singleton services of type <typeparamref name="TImplemented"/> and explicitly defined interfaces.
        /// </summary>
        public static IServiceCollection AddInterfacesAsSingleton<TImplemented, T1, T2>(this IServiceCollection services)
            where T1 : class where T2 : class where TImplemented : class, T1, T2
        {
            return services.AddSingleton<T1, TImplemented>()
                .AddSingleton((sp) => (T2)(TImplemented)sp.GetRequiredService<T1>());
        }
    }
}
