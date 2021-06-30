using Hive.Models;
using GraphQL.Types;
using System;
using System.Collections.Generic;

namespace Hive.Graphing.Types
{
    /// <summary>
    /// The GQL representation of a <see cref="ModReference"/>.
    /// </summary>
    public class ModReferenceType : ObjectGraphType<ModReference>
    {
        /// <summary>
        /// Setup a ModReferenceType for GQL.
        /// </summary>
        public ModReferenceType(IEnumerable<ICustomHiveGraph<ModReferenceType>> customGraphs)
        {
            if (customGraphs is null)
                throw new ArgumentNullException(nameof(customGraphs));

            Name = nameof(ModReference);
            Description = Resources.GraphQL.ModReference;

            _ = Field(mr => mr.ModID)
                .Description(Resources.GraphQL.ModReference_ModID);

            _ = Field<StringGraphType>(
                "versionRange",
                Resources.GraphQL.ModReference_VersionRange,
                resolve: ctx => ctx.Source.Versions.ToString()
                );

            foreach (var graph in customGraphs)
                graph.Configure(this);
        }
    }
}
