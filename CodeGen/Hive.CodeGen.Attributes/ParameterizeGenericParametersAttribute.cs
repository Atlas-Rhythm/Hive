using System;

namespace Hive.CodeGen
{
    /// <summary>
    /// Specifies that a generic class will be automatically parameterized within the range specified.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
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
        /// <param name="min">The minimum number of generic parameters to parameterize with.</param>
        /// <param name="max">The maximum number of generic parameters to parameterize with.</param>
        public ParameterizeGenericParametersAttribute(int min, int max)
            => (MinParameters, MaxParameters) = (min, max);
    }

    /// <summary>
    /// Marks that a generic class is a generated parameterization of a type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
    public sealed class GeneratedParameterizationAttribute : Attribute
    {
        /// <summary>
        /// Gets the type name of the type it was parameterized from.
        /// </summary>
        public Type GeneratedFrom { get; }

        /// <summary>
        /// Gets the number of parameters this was instantiated with.
        /// </summary>
        public int WithParameters { get; }

        /// <summary>
        /// Constructs a <see cref="GeneratedParameterizationAttribute"/> with the specified generated source and parameter count.
        /// </summary>
        /// <param name="from">The type that this instantiation was generated from.</param>
        /// <param name="with">The number of parameters it was generated with.</param>
        public GeneratedParameterizationAttribute(Type from, int with)
            => (GeneratedFrom, WithParameters) = (from, with);
    }
}
