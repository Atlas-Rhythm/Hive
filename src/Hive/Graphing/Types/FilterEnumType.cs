using System;
using System.Collections.Generic;
using GraphQL.Types;

namespace Hive.Graphing.Types
{
    /// <summary>
    /// A type for filtering enums.
    /// </summary>
    public class FilterEnumType : EnumerationGraphType<ModType.Filter>
    {
        /// <summary>
        /// Creates an enum filter.
        /// </summary>
        /// <param name="customGraphs"></param>
        public FilterEnumType(IEnumerable<ICustomHiveGraph<FilterEnumType>> customGraphs)
        {
            if (customGraphs is null)
                throw new ArgumentNullException(nameof(customGraphs));

            Description = "The different order filters for querying mods.";

            foreach (var graph in customGraphs)
                graph.Configure(this);
        }
    }
}
