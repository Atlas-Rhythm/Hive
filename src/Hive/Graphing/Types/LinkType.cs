using System;
using System.Collections.Generic;
using GraphQL.Types;

namespace Hive.Graphing.Types
{
    /// <summary>
    /// The GQL representation of a link on a <see cref="Models.Mod"/>. Specifically from <seealso cref="Models.Mod.Links"/>.
    /// </summary>
    public class LinkType : ObjectGraphType<(string, Uri)>
    {
        /// <summary>
        /// Setup a LinkType for GQL.
        /// </summary>
        public LinkType(IEnumerable<ICustomHiveGraph<LinkType>> customGraphs)
        {
            if (customGraphs is null)
                throw new ArgumentNullException(nameof(customGraphs));

            Name = "Link";
            Description = Resources.GraphQL.Link;

            _ = Field(l => l.Item1)
                .Name("name")
                .Description(Resources.GraphQL.Link_Name);

            _ = Field<StringGraphType>(
                "url",
                Resources.GraphQL.Link_URL,
                resolve: context => context.Source.Item2.ToString()
            );

            foreach (var graph in customGraphs)
                graph.Configure(this);
        }
    }
}
