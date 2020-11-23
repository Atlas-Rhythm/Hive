using Hive.Models;
using GraphQL.Types;

namespace Hive.Graphing.Types
{
    public class LocalizedModInfoType : ObjectGraphType<LocalizedModInfo>
    {
        public LocalizedModInfoType()
        {
            Name = nameof(LocalizedModInfo);
            Description = Resources.GraphQL.LocalizedModInfo;

            Field(lmi => lmi.Name)
                .Description(Resources.GraphQL.LocalizedModInfo_Name);

            Field(lmi => lmi.Description)
                .Description(Resources.GraphQL.LocalizedModInfo_Description);

            Field(lmi => lmi.Changelog, nullable: true)
                .Description(Resources.GraphQL.LocalizedModInfo_Changelog);

            Field(lmi => lmi.Credits, nullable: true)
                .Description(Resources.GraphQL.LocalizedModInfo_Credits);
        }
    }
}