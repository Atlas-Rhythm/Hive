using System;
using Hive.CodeGen;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Graphing
{
    /// <summary>
    /// A utility class for resolving multiple services at once through generic types.
    /// </summary>
    public static partial class GenericResolver
    {
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
        [ParameterizeGenericParameters(1, 5)]
        public static ValueTuple<T1, T2, T3, T4, T5, T6> GetRequiredServices<T1, T2, T3, T4, T5, T6>(this IServiceProvider provider) where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull
            => new(provider.GetRequiredService<T1>(), provider.GetRequiredService<T2>(), provider.GetRequiredService<T3>(), provider.GetRequiredService<T4>(), provider.GetRequiredService<T5>(), provider.GetRequiredService<T6>());
    }
}
