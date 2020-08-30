using Hive.Utilities;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Hive.Permissions
{
    /// <summary>
    /// A permission rule.
    /// </summary>
    /// <remarks>
    /// <para><see cref="IRuleProvider"/> implemeters may extend <see cref="Rule"/> to store additional data.</para>
    /// <para>The same <see cref="Rule"/> object may not be given to two instances of <see cref="PermissionsManager{TContext}"/>
    /// with different context types.</para>
    /// </remarks>
    public class Rule
    {
        /// <summary>
        /// Gets the name of the rule.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Gets the definition of the rule.
        /// </summary>
        public string Definition { get; }

        /// <summary>
        /// Constructs a <see cref="Rule"/> with the specified name and definition.
        /// </summary>
        /// <param name="name">The name of the rule.</param>
        /// <param name="def">The definition of the rule.</param>
        public Rule(string name, string def)
        {
            Name = name;
            Definition = def;
        }

        // This is used as a cache for the PermissionsManager.
        internal Delegate? Compiled = null;
        internal Instant CompiledAt = Instant.MinValue;
    }

    /// <summary>
    /// A generic <see cref="Rule"/> that allows storage of some custom data.
    /// </summary>
    /// <typeparam name="TCustom">The type of custom data to store.</typeparam>
    public sealed class Rule<TCustom> : Rule
    {
        /// <summary>
        /// Gets the custom data for this rule.
        /// </summary>
        public TCustom Data { get; }

        /// <summary>
        /// Constructs a rule with the given name, definition, and custom data.
        /// </summary>
        /// <param name="name">The name of the rule.</param>
        /// <param name="def">The definition of the rule.</param>
        /// <param name="data">The custom data to store on this object.</param>
        public Rule(string name, string def, TCustom data) : base(name, def)
        {
            Data = data;
        }
    }
}
