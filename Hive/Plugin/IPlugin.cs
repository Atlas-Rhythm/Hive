using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Plugin
{
    public interface IAggregate<out T> where T : IPlugin
    {
        T Combine();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "We use IPlugin as an ensurance for IAggreagator.")]
    public interface IPlugin
    {
    }
}