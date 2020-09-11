using System;
using System.Collections.Generic;
using System.Text;

namespace Hive.Plugins
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class AggregableAttribute : Attribute
    {
    }
}
