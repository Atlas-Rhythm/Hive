using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Hive
{
    /// <summary>
    /// A special <see cref="ControllerFeatureProvider"/> that allows for MVC controllers to be conditional.
    /// </summary>
    public class HiveConditionalControllerFeatureProvider : ControllerFeatureProvider
    {
        // The default condition that will be applied to MVC controllers without a defined condition.
        private readonly Func<bool> defaultConditionFunc;

        // A dictionary of MVC controller types to their own specific conditions
        private readonly Dictionary<Type, Func<bool>> controllerConditionDictionary = new();

        /// <summary>
        /// Create a new <see cref="HiveConditionalControllerFeatureProvider"/> with a resolved true/false value
        /// as the default condition.
        /// </summary>
        /// <param name="defaultCondition">The default condition for MVC controllers to be loaded.</param>
        public HiveConditionalControllerFeatureProvider(bool defaultCondition = true) : this(() => defaultCondition) { }

        /// <summary>
        /// Create a new <see cref="HiveConditionalControllerFeatureProvider"/> with a function that
        /// controls whether or not MVC controllers can be loaded.
        /// </summary>
        /// <param name="defaultConditionFunc">A </param>
        public HiveConditionalControllerFeatureProvider(Func<bool> defaultConditionFunc) => this.defaultConditionFunc = defaultConditionFunc;

        /// <summary>
        /// Registers an MVC Controller with a resolved <see cref="bool"/> that controls whether or not it will load.
        /// </summary>
        /// <typeparam name="T">Type that will be matched with the provided condition.</typeparam>
        /// <param name="condition">A resolved <see cref="bool"/> that controls whether or not the MVC controller will load.</param>
        /// <returns></returns>
        public HiveConditionalControllerFeatureProvider RegisterCondition<T>(bool condition) where T : ControllerBase
            => RegisterCondition<T>(() => condition);

        /// <summary>
        /// Registers an MVC Controller with a condition that controls whether or not it will load.
        /// </summary>
        /// <typeparam name="T">Type that will be matched with the provided condition.</typeparam>
        /// <param name="condition">A function that controls whether or not the MVC controller will load.</param>
        public HiveConditionalControllerFeatureProvider RegisterCondition<T>(Func<bool> condition) where T : ControllerBase
        {
            controllerConditionDictionary.Add(typeof(T).GetTypeInfo(), condition);
            return this;
        }

        /// <inheritdoc/>
        protected override bool IsController(TypeInfo typeInfo)
            => base.IsController(typeInfo) && (controllerConditionDictionary.TryGetValue(typeInfo, out var conditionFunc)
                ? conditionFunc()
                : defaultConditionFunc());
    }
}
