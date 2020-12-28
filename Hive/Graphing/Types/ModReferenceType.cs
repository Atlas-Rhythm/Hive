using Hive.Models;
using GraphQL.Types;

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
        public ModReferenceType()
        {
            Name = nameof(ModReference);
            Description = Resources.GraphQL.ModReference;

            _ = Field(mr => mr.ModID)
                .Description(Resources.GraphQL.ModReference_ModID);

            _ = Field<StringGraphType>(
                "versionRange",
                Resources.GraphQL.ModReference_VersionRange,
                resolve: ctx => ctx.Source.Versions.ToString()
                );
        }
    }
}
