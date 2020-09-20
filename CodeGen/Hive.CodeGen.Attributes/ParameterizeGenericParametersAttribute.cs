using System;
using System.CodeDom.Compiler;

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

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
    public sealed class GeneratedParameterizationAttribute : Attribute
    {
        public string GeneratedFrom { get; }
        public int WithParameters { get; }

        public GeneratedParameterizationAttribute(string from, int with)
            => (GeneratedFrom, WithParameters) = (from, with);
    }
}