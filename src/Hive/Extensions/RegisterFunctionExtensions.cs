using DryIoc;
using System;

namespace Hive.Extensions
{
    /// <summary>
    /// Extensions for <see cref="IContainer"/> for registering custom functions to the Permissions System.
    /// </summary>
    public static class RegisterFunctionExtensions
    {
        /// <summary>
        /// Registers a custom builtin function to the Permission System.
        /// </summary>
        /// <remarks>
        /// Shorthand for <code>IContainer.RegisterInstance((name, impl))</code>
        /// </remarks>
        /// <param name="container">The <see cref="IContainer"/> to inject our function into.</param>
        /// <param name="name">Function name</param>
        /// <param name="impl">Function implementation</param>
        public static void RegisterBuiltinFunction(this IContainer container, string name, Delegate impl)
            => container.RegisterInstance((name, impl));
    }
}
