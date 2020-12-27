using Hive.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Hive.Extensions
{
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
    }
}
