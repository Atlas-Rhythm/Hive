using Hive.Models;
using GraphQL.Types;
using System;
using System.Collections.Generic;

namespace Hive.Graphing.Types
{
    /// <summary>
    /// The GQL representation of a <see cref="GameVersion"/>.
    /// </summary>
    public class GameVersionType : ObjectGraphType<GameVersion>
    {
        /// <summary>
        /// Setup a GameVersionType for GQL.
        /// </summary>
        public GameVersionType(IEnumerable<ICustomHiveGraph<GameVersionType>> customGraphs)
        {
            if (customGraphs is null)
                throw new ArgumentNullException(nameof(customGraphs));

            Name = nameof(GameVersion);
            Description = Resources.GraphQL.GameVersion;

            _ = Field(gv => gv.Name)
                .Description(Resources.GraphQL.GameVersion_Name);

            _ = Field<StringGraphType>(
                "creationTime",
                Resources.GraphQL.GameVersion_CreationTime,
                resolve: ctx => ctx.Source!.CreationTime.ToString());

            foreach (var graph in customGraphs)
                graph.Configure(this);
        }
    }
}
