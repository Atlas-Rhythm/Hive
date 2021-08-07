using System;
using System.Collections.Generic;
using GraphQL.Types;
using Hive.Models;

namespace Hive.Graphing.Types
{
    /// <summary>
    /// The GQL representation of a <see cref="Mod"/>.
    /// </summary>
    public class ModType : ObjectGraphType<Mod>
    {
        /// <summary>
        /// Setup a ModType for GQL.
        /// </summary>
        public ModType(IEnumerable<ICustomHiveGraph<ModType>> customGraphs)
        {
            if (customGraphs is null)
                throw new ArgumentNullException(nameof(customGraphs));

            Name = nameof(Mod);
            Description = Resources.GraphQL.Mod;

            _ = Field(m => m.Id)
                .Description(Resources.GraphQL.Mod_ID);

            _ = Field(m => m.ReadableID)
                .Description(Resources.GraphQL.Mod_ReadableID);

            _ = Field<ListGraphType<LocalizedModInfoType>>(
                "localizations",
                Resources.GraphQL.Mod_Localizations,
                resolve: ctx => ctx.Source.Localizations);

            _ = Field<StringGraphType>(
                "version",
                Resources.GraphQL.Mod_Version,
                resolve: ctx => ctx.Source.Version.ToString());

            _ = Field<StringGraphType>(
                "uploadedAt",
                Resources.GraphQL.Mod_UploadedAt,
                resolve: ctx => ctx.Source.UploadedAt.ToString());

            _ = Field<StringGraphType>(
                "editedAt",
                Resources.GraphQL.Mod_EditedAt,
                resolve: ctx => ctx.Source.EditedAt.ToString());

            _ = Field<UserType>(
                "uploader",
                Resources.GraphQL.Mod_Uploader,
                resolve: ctx => ctx.Source.Uploader);

            _ = Field<ListGraphType<UserType>>(
                "authors",
                Resources.GraphQL.Mod_Authors,
                resolve: ctx => ctx.Source.Authors);

            _ = Field<ListGraphType<UserType>>(
                "contributors",
                Resources.GraphQL.Mod_Contributors,
                resolve: ctx => ctx.Source.Contributors);

            _ = Field<ListGraphType<GameVersionType>>(
                "supportedVersions",
                Resources.GraphQL.Mod_SupportedVersions,
                resolve: ctx => ctx.Source.SupportedVersions);

            _ = Field<ListGraphType<ModReferenceType>>(
                "dependencies",
                Resources.GraphQL.Mod_Dependencies,
                resolve: ctx => ctx.Source.Dependencies);

            _ = Field<ListGraphType<ModReferenceType>>(
                "conflicts",
                Resources.GraphQL.Mod_Conflicts,
                resolve: ctx => ctx.Source.Conflicts);

            _ = Field<ListGraphType<LinkType>>(
                "links",
                Resources.GraphQL.Mod_Links,
                resolve: (context) => context.Source.Links);

            _ = Field<StringGraphType>(
                "downloadLink",
                Resources.GraphQL.Mod_DownloadLink,
                resolve: context => context.Source.DownloadLink.ToString()
            );

            foreach (var graph in customGraphs)
                graph.Configure(this);
        }

        /// <summary>
        /// The different order filters for querying mods.
        /// </summary>
        public enum Filter
        {
            /// <summary>
            /// Get all versions of a mod.
            /// </summary>
            All,

            /// <summary>
            /// Get the most recent version of a mod.
            /// </summary>
            Recent,

            /// <summary>
            /// Get the latest version of a mod.
            /// </summary>
            Latest
        }
    }
}
