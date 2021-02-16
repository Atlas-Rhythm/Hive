using System;

namespace Hive.Plugins
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class PluginStartupAttribute : Attribute
    {
    }
}
