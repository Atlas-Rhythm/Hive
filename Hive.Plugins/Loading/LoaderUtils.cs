using System;
using System.Collections.Generic;
using System.Reflection;
using Hive.Plugins.Resources;
using Hive.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Plugins.Loading
{
    /// <summary>
    /// A type containing utilities used by the plugin loader.
    /// </summary>
    public static class LoaderUtils
    {
        /// <summary>
        /// Invokes the method with the specified target object and argument list without wrapping thrown exceptions.
        /// </summary>
        /// <param name="method">The method to call.</param>
        /// <param name="obj">The object instance for the method to target, or <see langword="null"/> if the method is static.</param>
        /// <param name="arguments">The list of arguments to invoke the method with.</param>
        /// <returns>The value the method returned, if any.</returns>
        public static object? InvokeWithoutWrappingExceptions(this MethodInfo method, object? obj, object?[] arguments)
            => (method ?? throw new ArgumentNullException(nameof(method))).Invoke(obj, BindingFlags.DoNotWrapExceptions, null, arguments, null);

        /// <summary>
        /// Creates a delegate to invoke a method using arguments populated from an <see cref="IServiceProvider"/>, optionally providing
        /// a set of arguments to provide for the early values.
        /// </summary>
        /// <remarks>
        /// Parameter injection is done when this method is called, not when its returned delegate is called.
        /// </remarks>
        /// <param name="services">The <see cref="IServiceProvider"/> to use to populate the method's arguments.</param>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arguments">A list of arguments to pass before any arguments are injected, or <see langword="null"/> if none are needed.</param>
        /// <returns>A delegate that takes the target object of the invocation and invokes the method with the injected parameters, discarding its result.</returns>
        public static Action<object?> InjectVoidMethod(this IServiceProvider services, MethodInfo method, object?[]? arguments)
        {
            var invoke = InjectMethod(services, method, arguments);
            return thisobj => _ = invoke(thisobj);
        }

        /// <summary>
        /// Creates a delegate to invoke a method using arguments populated from an <see cref="IServiceProvider"/>, with a function to provide additional services
        /// not given by the service provider, optionally providing a set of arguments to provide for the early values.
        /// </summary>
        /// <remarks>
        /// Parameter injection is done when this method is called, not when its returned delegate is called.
        /// </remarks>
        /// <param name="services">The <see cref="IServiceProvider"/> to use to populate the method's arguments.</param>
        /// <param name="method">The method to invoke.</param>
        /// <param name="serviceOverride">A delegate that will recieve the type of a parameter, and can either return a value to have it injected, or <see langword="null"/>
        /// to use the normally injected value.</param>
        /// <param name="arguments">A list of arguments to pass before any arguments are injected, or <see langword="null"/> if none are needed.</param>
        /// <returns>A delegate that takes the target object of the invocation and invokes the method with the injected parameters, discarding its result.</returns>
        public static Action<object?> InjectVoidMethod(this IServiceProvider services, MethodInfo method, Func<Type, object?> serviceOverride, object?[]? arguments)
        {
            var invoke = InjectMethod(services, method, serviceOverride, arguments);
            return thisobj => _ = invoke(thisobj);
        }

        /// <summary>
        /// Creates a delegate to invoke a method using arguments populated from an <see cref="IServiceProvider"/>, optionally providing
        /// a set of arguments to provide for the early values.
        /// </summary>
        /// <remarks>
        /// Parameter injection is done when this method is called, not when its returned delegate is called.
        /// </remarks>
        /// <param name="services">The <see cref="IServiceProvider"/> to use to populate the method's arguments.</param>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arguments">A list of arguments to pass before any arguments are injected, or <see langword="null"/> if none are needed.</param>
        /// <returns>A delegate that takes the target object of the invocation and invokes the method with the injected parameters, and returns its result.</returns>
        public static Func<object?, object?> InjectMethod(this IServiceProvider services, MethodInfo method, object?[]? arguments)
            => InjectMethod(services, method, _ => null, arguments);


        /// <summary>
        /// Creates a delegate to invoke a method using arguments populated from an <see cref="IServiceProvider"/>, with a function to provide additional services
        /// not given by the service provider, optionally providing a set of arguments to provide for the early values.
        /// </summary>
        /// <remarks>
        /// Parameter injection is done when this method is called, not when its returned delegate is called.
        /// </remarks>
        /// <param name="services">The <see cref="IServiceProvider"/> to use to populate the method's arguments.</param>
        /// <param name="method">The method to invoke.</param>
        /// <param name="serviceOverride">A delegate that will recieve the type of a parameter, and can either return a value to have it injected, or <see langword="null"/>
        /// to use the normally injected value.</param>
        /// <param name="arguments">A list of arguments to pass before any arguments are injected, or <see langword="null"/> if none are needed.</param>
        /// <returns>A delegate that takes the target object of the invocation and invokes the method with the injected parameters, and returns its result.</returns>
        public static Func<object?, object?> InjectMethod(this IServiceProvider services, MethodInfo method, Func<Type, object?> serviceOverride, object?[]? arguments)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));
            if (method is null)
                throw new ArgumentNullException(nameof(method));
            if (serviceOverride is null)
                throw new ArgumentNullException(nameof(serviceOverride));

            arguments ??= Array.Empty<object?>();
            var parameters = method.GetParameters();
            if (arguments.Length > parameters.Length)
                throw new ArgumentException(SR.Utils_Inject_TooManyArguments.Format(method.Name, method.DeclaringType?.FullName), nameof(arguments));
            var args = new object?[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                args[i] = serviceOverride(param.ParameterType) ?? InjectParameter(services, method, param, arguments, i);
            }
            return thisobj => method.InvokeWithoutWrappingExceptions(thisobj, args);
        }

        /// <summary>
        /// Safely gets all types that can be loaded from an assembly.
        /// </summary>
        /// <remarks>
        /// This method swallows all type load errors to return as many types as it can.
        /// </remarks>
        /// <param name="assembly">The assembly to get the types from.</param>
        /// <returns>A sequence containing all types that could be loaded from the assembly.</returns>
        public static IEnumerable<Type> SafeGetTypes(this Assembly assembly)
        {
            if (assembly is null)
                throw new ArgumentNullException(nameof(assembly));

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.WhereNotNull();
            }
        }

        private static object? InjectParameter(IServiceProvider services, MethodInfo method, ParameterInfo param, object?[] givenArguments, int i)
        {
            if (i < givenArguments.Length)
            {
                return givenArguments[i];
            }

            try
            {
                return services.GetRequiredService(param.ParameterType);
            }
            catch (Exception e)
            {
                throw new ServiceResolutionException(SR.Utils_Inject_CouldNotResolveService.Format(
                        param.ParameterType.FullName,
                        param.Name,
                        method.Name,
                        method.DeclaringType?.FullName
                    ), e);
            }
        }
    }
}
