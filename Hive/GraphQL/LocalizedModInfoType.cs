using Hive.Models;
using GraphQL.Types;

namespace Hive.GraphQL
{
    public class LocalizedModInfoType : ObjectGraphType<LocalizedModInfo>
    {
        public LocalizedModInfoType()
        {
            Name = "LocalizedModInfo";
            Field(lmi => lmi.Name).Description(Resources.GraphQL.LocalizedModInfo_Name);
            Field(lmi => lmi.Description).Description(Resources.GraphQL.LocalizedModInfo_Description);
            Field(lmi => lmi.Changelog).Description(Resources.GraphQL.LocalizedModInfo_Changelog);
            Field(lmi => lmi.Credits).Description(Resources.GraphQL.LocalizedModInfo_Credits);
            Field<ModType>(
                "owningMod",
                Resources.GraphQL.LocalizedModInfo_OwningMod,
                resolve: (context) => context.Source.OwningMod);
        }
    }
}