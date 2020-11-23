using System;
using GraphQL.Types;

namespace Hive.Graphing.Types
{
    public class LinkType : ObjectGraphType<(string, Uri)>
    {
        public LinkType()
        {
            Name = "Link";
            Description = Resources.GraphQL.Link;

            Field(l => l.Item1)
                .Name("name")
                .Description(Resources.GraphQL.Link_Name);

            Field<StringGraphType>(
                "url",
                Resources.GraphQL.Link_URL,
                resolve: context => context.Source.Item2.ToString()
            );
        }
    }
}