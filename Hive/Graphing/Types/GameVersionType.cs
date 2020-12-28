using Hive.Models;
using GraphQL.Types;

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
        public GameVersionType()
        {
            Name = nameof(GameVersion);
            Description = Resources.GraphQL.GameVersion;

            _ = Field(gv => gv.Name)
                .Description(Resources.GraphQL.GameVersion_Name);

            _ = Field<StringGraphType>(
                "creationTime",
                Resources.GraphQL.GameVersion_CreationTime,
                resolve: ctx => ctx.Source.CreationTime.ToString());
        }
    }
}
