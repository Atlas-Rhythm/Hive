using System;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Graphing
{
    /// <summary>
    /// A utility class for resolving multiple services at once through generic types.
    /// </summary>
    public static class GenericResolver
    {
        /// <summary>
        /// Resolves two services at once.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="provider">The provider for the services.</param>
        /// <returns>The resolved services.</returns>
        public static Tuple<T1, T2> GetRequiredServices<T1, T2>(this IServiceProvider provider) where T1 : notnull where T2 : notnull => new Tuple<T1, T2>(provider.GetRequiredService<T1>(), provider.GetRequiredService<T2>());

        /// <summary>
        /// Resolves three services at once.
        /// </summary>
        /// <typeparam name="T1">The type of a service to be resolved.</typeparam>
        /// <typeparam name="T2">The type of a service to be resolved.</typeparam>
        /// <typeparam name="T3">The type of a service to be resolved.</typeparam>
        /// <param name="provider">The provider for the services.</param>
        /// <returns>The resolved services.</returns>
        public static Tuple<T1, T2, T3> GetRequiredServices<T1, T2, T3>(this IServiceProvider provider) where T1 : notnull where T2 : notnull where T3 : notnull => new Tuple<T1, T2, T3>(provider.GetRequiredService<T1>(), provider.GetRequiredService<T2>(), provider.GetRequiredService<T3>());

        /// <summary>
        /// Resolves four services at once.
        /// </summary>
        /// <typeparam name="T1">The type of a service to be resolved.</typeparam>
        /// <typeparam name="T2">The type of a service to be resolved.</typeparam>
        /// <typeparam name="T3">The type of a service to be resolved.</typeparam>
        /// <typeparam name="T4">The type of a service to be resolved.</typeparam>
        /// <param name="provider">The provider for the services.</param>
        /// <returns>The resolved services.</returns>
        public static Tuple<T1, T2, T3, T4> GetRequiredServices<T1, T2, T3, T4>(this IServiceProvider provider) where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull => new Tuple<T1, T2, T3, T4>(provider.GetRequiredService<T1>(), provider.GetRequiredService<T2>(), provider.GetRequiredService<T3>(), provider.GetRequiredService<T4>());

        /// <summary>
        /// Resolves five services at once.
        /// </summary>
        /// <typeparam name="T1">The type of a service to be resolved.</typeparam>
        /// <typeparam name="T2">The type of a service to be resolved.</typeparam>
        /// <typeparam name="T3">The type of a service to be resolved.</typeparam>
        /// <typeparam name="T4">The type of a service to be resolved.</typeparam>
        /// <typeparam name="T5">The type of a service to be resolved.</typeparam>
        /// <param name="provider">The provider for the services.</param>
        /// <returns>The resolved services.</returns>
        public static Tuple<T1, T2, T3, T4, T5> GetRequiredServices<T1, T2, T3, T4, T5>(this IServiceProvider provider) where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull => new Tuple<T1, T2, T3, T4, T5>(provider.GetRequiredService<T1>(), provider.GetRequiredService<T2>(), provider.GetRequiredService<T3>(), provider.GetRequiredService<T4>(), provider.GetRequiredService<T5>());

        /// <summary>
        /// Resolves six services at once.
        /// </summary>
        /// <typeparam name="T1">The type of a service to be resolved.</typeparam>
        /// <typeparam name="T2">The type of a service to be resolved.</typeparam>
        /// <typeparam name="T3">The type of a service to be resolved.</typeparam>
        /// <typeparam name="T4">The type of a service to be resolved.</typeparam>
        /// <typeparam name="T5">The type of a service to be resolved.</typeparam>
        /// <typeparam name="T6">The type of a service to be resolved.</typeparam>
        /// <param name="provider">The provider for the services.</param>
        /// <returns>The resolved services.</returns>
        public static Tuple<T1, T2, T3, T4, T5, T6> GetRequiredServices<T1, T2, T3, T4, T5, T6>(this IServiceProvider provider) where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull => new Tuple<T1, T2, T3, T4, T5, T6>(provider.GetRequiredService<T1>(), provider.GetRequiredService<T2>(), provider.GetRequiredService<T3>(), provider.GetRequiredService<T4>(), provider.GetRequiredService<T5>(), provider.GetRequiredService<T6>());
    }
}
