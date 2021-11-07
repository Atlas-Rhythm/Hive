using System;

namespace Hive.CodeGen
{
    /// <summary>
    /// Specifies that a generic class will be automatically parameterized within the range specified.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ParameterizeGenericParametersAttribute : Attribute
    {
        /// <summary>
        /// Gets the minimum number of generic parameters to generate.
        /// </summary>
        public int MinParameters { get; }

        /// <summary>
        /// Gets the maximum number of generic parameters to generate.
        /// </summary>
        public int MaxParameters { get; }

        /// <summary>
        /// Constructs a <see cref="ParameterizeGenericParametersAttribute"/> with the specified minimum and maximum generic parameters
        /// to generate.
        /// </summary>
        /// <param name="minParameters">The minimum number of generic parameters to parameterize with.</param>
        /// <param name="maxParameters">The maximum number of generic parameters to parameterize with.</param>
        public ParameterizeGenericParametersAttribute(int minParameters, int maxParameters)
            => (MinParameters, MaxParameters) = (minParameters, maxParameters);
    }

    /// <summary>
    /// Marks that a generic class is a generated parameterization of a type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class GeneratedParameterizationAttribute : Attribute
    {
        /// <summary>
        /// Gets the type name of the type it was parameterized from, if this is applied to a generated type.
        /// </summary>
        public Type? GeneratedFrom { get; }

        /// <summary>
        /// Gets the number of parameters this was instantiated with.
        /// </summary>
        public int WithParameters { get; }

        /// <summary>
        /// Constructs a <see cref="GeneratedParameterizationAttribute"/> with the specified generated source and parameter count.
        /// </summary>
        /// <param name="generatedFrom">The type that this instantiation was generated from.</param>
        /// <param name="withParameters">The number of parameters it was generated with.</param>
        public GeneratedParameterizationAttribute(Type? generatedFrom, int withParameters)
            => (GeneratedFrom, WithParameters) = (generatedFrom, withParameters);
    }
}
