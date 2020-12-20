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

            _ = Field(lmi => lmi.Name)
                .Description(Resources.GraphQL.LocalizedModInfo_Name);

            _ = Field(lmi => lmi.Description)
                .Description(Resources.GraphQL.LocalizedModInfo_Description);

            _ = Field(lmi => lmi.Changelog, nullable: true)
                .Description(Resources.GraphQL.LocalizedModInfo_Changelog);

            _ = Field(lmi => lmi.Credits, nullable: true)
                .Description(Resources.GraphQL.LocalizedModInfo_Credits);
        }
    }
}
