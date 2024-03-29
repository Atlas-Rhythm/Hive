﻿using Hive.Models;
using Hive.Models.Serialized;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Hive.Extensions
{
    /// <summary>
    /// Extensions for the <see cref="Mod"/>.
    /// </summary>
    public static class ModExtensions
    {
        /// <summary>
        /// Return the first <see cref="LocalizedModInfo"/> that belongs to a <see cref="Mod"/> that fits into a list of preferred languages.
        /// </summary>
        /// <remarks>
        /// If <paramref name="preferredLanguages"/> is null, or is an empty enumerable, Hive will search for the system language.
        /// If no <see cref="LocalizedModInfo"/>s exist that match the preferred languages, Hive will default to the first <see cref="LocalizedModInfo"/> attached to the <see cref="Mod"/>.
        /// </remarks>
        /// <param name="mod">The <see cref="Mod"/> object to search for</param>
        /// <param name="preferredLanguages">A list of preferred languages to search for, ordered by descending preference.</param>
        /// <returns>The first <see cref="LocalizedModInfo"/> of a <see cref="Mod"/> that matches the preferred languages, or the first attached to the <see cref="Mod"/>.</returns>
        public static LocalizedModInfo GetLocalizedInfo(this Mod mod, IEnumerable<string>? preferredLanguages)
        {
            if (mod is null)
            {
                throw new ArgumentException($"Mod is null.");
            }

            if (preferredLanguages is null || !preferredLanguages.Any())
            {
                preferredLanguages = new[] { CultureInfo.CurrentCulture.ToString() };
            }

            // If the plugins allow us to access this mod, we then perform a search on localized data to grab what we need.
            LocalizedModInfo? localizedModInfo = null;

            // Just cache all localizations for the mod we're looking for.
            var localizations = mod.Localizations;

            // We loop through each preferred language first, as they are what the user asked for.
            // This list is already sorted by quality values, so none should be needed.
            // We do not need to explicitly search for the System culture since it was already added to the end of this list.
            foreach (var preferredLanguage in preferredLanguages)
            {
                // TODO: also try to do this filtering on the DB
                var localizedInfos = localizations.Where(x => x.Language == preferredLanguage);
                if (localizedInfos.Any())
                {
                    localizedModInfo = localizedInfos.First();
                    break;
                }
            }

            // If no preferred languages were found, but localized data still exists, we grab the first that was found.
            if (localizedModInfo is null && localizations.Any())
            {
                localizedModInfo = localizations.First();
            }

            // If we still have no language, then... fuck.
            return localizedModInfo is null
                ? throw new ArgumentException($"Mod {mod.ReadableID} does not have any LocalizedModInfos attached to it.")
                : localizedModInfo;
        }

        /// <summary>
        /// Serialize a <see cref="HiveObjectQuery{T}"/> with a value of type <see cref="Mod"/> to a <see cref="SerializedMod"/>.
        /// </summary>
        /// <param name="query">The query to convert.</param>
        /// <param name="languageCultures">The languages to use in the conversion.</param>
        /// <returns>The wrapped <see cref="SerializedMod"/>.</returns>
        public static ActionResult<SerializedMod> Serialize(this HiveObjectQuery<Mod> query, IEnumerable<string> languageCultures)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query), "Object query is null");
            return query.Convert(mod => SerializedMod.Serialize(mod, mod.GetLocalizedInfo(languageCultures)));
        }

        /// <summary>
        /// Serializes a <see cref="HiveObjectQuery{T}"/> of a collection of <see cref="Mod"/> objects to a collection of <see cref="SerializedMod"/> objects.
        /// </summary>
        /// <param name="query">The query to convert.</param>
        /// <param name="languageCultures">The languages to use in the conversion.</param>
        /// <returns>The wrapped collection of <see cref="SerializedMod"/> objects.</returns>
        public static ActionResult<IEnumerable<SerializedMod>> Serialize(this HiveObjectQuery<IEnumerable<Mod>> query, IEnumerable<string> languageCultures)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query), "Object query is null");
            return query.Convert(mods => mods.Select(mod => SerializedMod.Serialize(mod, mod.GetLocalizedInfo(languageCultures))));
        }
    }
}
