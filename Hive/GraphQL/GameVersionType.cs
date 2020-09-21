using Hive.Models;
using GraphQL.Types;

namespace Hive.GraphQL
{
    public class GameVersionType : ObjectGraphType<GameVersion>
    {
        public GameVersionType()
        {
            Name = "GameVersion";
            Field(gv => gv.Name).Description(Resources.GraphQL.GameVersion_Name);
            Field<DateTimeGraphType>(
                "creationTime",
                Resources.GraphQL.GameVersion_CreationTime,
                resolve: context => context.Source.CreationTime.ToDateTimeUtc()
            );
            Field<ListGraphType<ModType>>(
                "supportedMods",
                Resources.GraphQL.GameVersion_SupportedMods,
                resolve: context => context.Source.SupportedMods);
        }
    }
}