using System;
using System.Collections.Generic;
using System.Text;

namespace Hive.CodeGen
{
    /// <summary>
    /// Indicates that a type or member is generated.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public sealed class IsGeneratedAttribute : Attribute
    {
    }
}
