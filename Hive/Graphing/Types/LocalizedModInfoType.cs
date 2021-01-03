using Hive.Models;
using GraphQL.Types;

namespace Hive.Graphing.Types
{
    /// <summary>
    /// The GQL representation of a <see cref="LocalizedModInfo"/>.
    /// </summary>
    public class LocalizedModInfoType : ObjectGraphType<LocalizedModInfo>
    {
        /// <summary>
        /// Setup a LocalizedModInfoType for GQL.
        /// </summary>
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
