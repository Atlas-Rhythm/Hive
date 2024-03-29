﻿using Microsoft.Extensions.DependencyInjection;

namespace Hive.Extensions
{
    /// <summary>
    /// Extensions for <see cref="IServiceCollection"/>
    /// </summary>
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds scoped services of type <typeparamref name="TImplemented"/> and explicitly defined interfaces.
        /// </summary>
        public static IServiceCollection AddInterfacesAsScoped<TImplemented, T1, T2>(this IServiceCollection services)
            where T1 : class where T2 : class where TImplemented : class, T1, T2
        {
            return services.AddScoped<T1, TImplemented>()
                .AddScoped((sp) => (T2)(TImplemented)sp.GetRequiredService<T1>());
        }
    }
}
