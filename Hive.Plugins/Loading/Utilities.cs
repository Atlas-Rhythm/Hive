﻿using System;
using System.Reflection;
using Hive.Plugins.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Plugins.Loading
{
    internal static class Utilities
    {
        public static object? InvokeWithoutWrappingExceptions(this MethodInfo method, object? obj, object?[] arguments)
            => method.Invoke(obj, BindingFlags.DoNotWrapExceptions, null, arguments, null);

        public static Action<object?> InjectVoidMethod(this IServiceProvider services, MethodInfo method, object?[]? arguments)
        {
            var invoke = InjectMethod(services, method, arguments);
            return thisobj => _ = invoke(thisobj);
        }

        public static Action<object?> InjectVoidMethod(this IServiceProvider services, MethodInfo method, Func<Type, object?> serviceOverride, object?[]? arguments)
        {
            var invoke = InjectMethod(services, method, serviceOverride, arguments);
            return thisobj => _ = invoke(thisobj);
        }

        public static Func<object?, object?> InjectMethod(this IServiceProvider services, MethodInfo method, object?[]? arguments)
            => InjectMethod(services, method, _ => null, arguments);

        public static Func<object?, object?> InjectMethod(this IServiceProvider services, MethodInfo method, Func<Type, object?> serviceOverride, object?[]? arguments)
        {
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
                throw new Exception(SR.Utils_Inject_CouldNotResolveService.Format(
                        param.ParameterType.FullName,
                        param.Name,
                        method.Name,
                        method.DeclaringType?.FullName
                    ), e);
            }
        }
    }
}