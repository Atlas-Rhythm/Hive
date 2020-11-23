using Hive.Models;
using GraphQL.Types;

namespace Hive.Graphing.Types
{
    public class ModType : ObjectGraphType<Mod>
    {
        public ModType()
        {
            Name = nameof(Mod);
            Description = Resources.GraphQL.Mod;

            Field(m => m.Id)
                .Description(Resources.GraphQL.Mod_ID);

            Field(m => m.ReadableID)
                .Description(Resources.GraphQL.Mod_ReadableID);

            Field<ListGraphType<LocalizedModInfoType>>(
                "localizations",
                Resources.GraphQL.Mod_Localizations,
                resolve: ctx => ctx.Source.Localizations);

            Field<StringGraphType>(
                "version",
                Resources.GraphQL.Mod_Version,
                resolve: ctx => ctx.Source.Version.ToString());

            Field<StringGraphType>(
                "uploadedAt",
                Resources.GraphQL.Mod_UploadedAt,
                resolve: ctx => ctx.Source.UploadedAt.ToString());

            Field<StringGraphType>(
                "editedAt",
                Resources.GraphQL.Mod_EditedAt,
                resolve: ctx => ctx.Source.EditedAt.ToString());

            Field<UserType>(
                "uploader",
                Resources.GraphQL.Mod_Uploader,
                resolve: ctx => ctx.Source.Uploader);

            Field<ListGraphType<UserType>>(
                "authors",
                Resources.GraphQL.Mod_Authors,
                resolve: ctx => ctx.Source.Authors);

            Field<ListGraphType<UserType>>(
                "contributors",
                Resources.GraphQL.Mod_Contributors,
                resolve: ctx => ctx.Source.Contributors);

            Field<ListGraphType<GameVersionType>>(
                "supportedVersions",
                Resources.GraphQL.Mod_SupportedVersions,
                resolve: ctx => ctx.Source.SupportedVersions);

            Field<ListGraphType<ModReferenceType>>(
                "dependencies",
                Resources.GraphQL.Mod_Dependencies,
                resolve: ctx => ctx.Source.Dependencies);

            Field<ListGraphType<ModReferenceType>>(
                "conflicts",
                Resources.GraphQL.Mod_Conflicts,
                resolve: ctx => ctx.Source.Conflicts);

            Field<ListGraphType<LinkType>>(
                "links",
                Resources.GraphQL.Mod_Links,
                resolve: (context) => context.Source.Links);

            Field<StringGraphType>(
                "downloadLink",
                Resources.GraphQL.Mod_DownloadLink,
                resolve: context => context.Source.DownloadLink.ToString()
            );
        }
    }
}