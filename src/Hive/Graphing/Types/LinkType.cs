using System;
using System.Collections.Generic;
using GraphQL.Types;
using Hive.Models;

namespace Hive.Graphing.Types
{
    /// <summary>
    /// The GQL representation of a link on a <see cref="Models.Mod"/>. Specifically from <seealso cref="Models.Mod.Links"/>.
    /// </summary>
    public class LinkType : ObjectGraphType<Link>
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

            _ = Field(l => l.Name)
                .Name("name")
                .Description(Resources.GraphQL.Link_Name);

            _ = Field<StringGraphType>(
                "location",
                Resources.GraphQL.Link_URL,
                resolve: context => context.Source.Location.ToString()
            );

            foreach (var graph in customGraphs)
                graph.Configure(this);
        }
    }
}
