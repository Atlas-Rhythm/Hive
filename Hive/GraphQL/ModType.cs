using GraphQL;
using NodaTime;
using Hive.Models;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;

namespace Hive.GraphQL
{
    public class ModType : ObjectGraphType<Mod>
    {
        public ModType()
        {
            Name = "Mod";
            Field(m => m.Id).Description(Resources.GraphQL.Mod_ID);
            Field(m => m.ReadableID).Description(Resources.GraphQL.Mod_ReadableID);
            // Should every locale be exposed?
            Field<ListGraphType<LocalizedModInfoType>>(
                "localizations",
                Resources.GraphQL.Mod_Localizations,
                resolve: (context) => context.Source.Localizations);
            Field<StringGraphType>(
                "modVersion",
                Resources.GraphQL.Mod_Version,
                resolve: context => context.Source.Version.ToString()
            );
            Field<DateTimeGraphType>(
                "uploadedAt",
                Resources.GraphQL.Mod_UploadedAt,
                resolve: context => context.Source.UploadedAt.ToDateTimeUtc()
            );
            Field<DateTimeGraphType>(
                name: "editedAt",
                description: Resources.GraphQL.Mod_EditedAt,
                resolve: (context) =>
                {
                    Instant? time = context.Source.EditedAt;
                    if (time.HasValue)
                    {
                        return time.Value.ToDateTimeUtc();
                    }
                    return null;
                }
            );
            Field<UserType>(
                "uploader",
                Resources.GraphQL.Mod_Uploader,
                resolve: (context) => context.Source.Uploader);
            Field<ListGraphType<UserType>>(
                "authors",
                Resources.GraphQL.Mod_Authors,
                resolve: (context) => context.Source.Authors);
            Field<ListGraphType<UserType>>(
                "contributors",
                Resources.GraphQL.Mod_Contributors,
                resolve: (context) => context.Source.Contributors);
            Field<ListGraphType<GameVersionType>>(
                "supportedVersions",
                Resources.GraphQL.Mod_SupportedVersions,
                resolve: (context) => context.Source.SupportedVersions
            );
            Field<ListGraphType<ModReferenceType>>(
                "dependencies",
                Resources.GraphQL.Mod_Dependencies,
                resolve: (context) => context.Source.Dependencies);
            Field<ListGraphType<ModReferenceType>>(
                "conflicts",
                Resources.GraphQL.Mod_Conflicts,
                resolve: (context) => context.Source.Conflicts);
            Field<ChannelType>(
                "channel",
                Resources.GraphQL.Mod_Channel,
                resolve: (context) => context.Source.Channel);
            Field<ListGraphType<LinkType>>(
                "links",
                Resources.GraphQL.Mod_Links,
                resolve: (context) => context.Source.Links);
            Field<StringGraphType>(
                "downloadLink",
                Resources.GraphQL.Mod_DownloadLink,
                resolve: context => context.Source.DownloadLink.ToString()
            );

            FieldAsync<LocalizedModInfoType>(
                "localizedData",
                arguments: new QueryArguments(
                    HiveArguments.Name(Resources.GraphQL.LocalizedModInfo_Name)
                ),
                resolve: async context =>
                {
                    HiveContext hiveContext = context.Resolve<HiveContext>();
                    string name = context.GetArgument<string>("name");

                    // Maybe return default language (english) if none is found for the requested language.
                    // Also case sensitive search.
                    return await hiveContext.ModLocalizations.FirstOrDefaultAsync(ml => ml.Name == name).ConfigureAwait(false);
                }
            );
        }
    }
}