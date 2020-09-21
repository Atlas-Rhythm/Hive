using Hive.Models;
using GraphQL.Types;

namespace Hive.GraphQL
{
    public class ModReferenceType : ObjectGraphType<ModReference>
    {
        public ModReferenceType()
        {
            Name = "ModReference";
            Field(mr => mr.ModID).Description(Resources.GraphQL.ModReference_ModID);
            Field<StringGraphType>(
                "versions",
                Resources.GraphQL.ModReference_Versions,
                resolve: context => context.Source.Versions.ToString()
            );
        }
    }
}