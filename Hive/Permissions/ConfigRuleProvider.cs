using Hive.Utilities;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Hive.Permissions
{
    /// <summary>
    /// The default rule provider for Hive, which utilizes the file system.
    /// </summary>
    public class ConfigRuleProvider : IRuleProvider
    {
        private readonly string ruleDirectory = Path.Combine(Environment.CurrentDirectory, "Rules");
        private readonly string defaultRuleDefinition = "next(true)";
        private readonly StringView splitToken = ".";

        // Rule name to information corresponding to the particular rule.
        private readonly Dictionary<string, Rule<FileInfo>> cachedFileInfos = new();

        /// <summary>
        /// Construct a rule provider via DI.
        /// </summary>
        /// <param name="overrideRuleDirectory">If not empty, the rule provider will treat this as the root rule directory.</param>
        /// <param name="defaultRuleDefinition">The default definition for any newly created rule.</param>
        /// <param name="splitToken">The token used to split rules.</param>
        /// <remarks>
        /// If <paramref name="defaultRuleDefinition"/> is null or empty, Hive will default to a "Rules" subfolder in the install folder.
        /// The <paramref name="splitToken"/> parameter, and the one given to <see cref="PermissionsManager{TContext}"/>, should always be the same.
        /// </remarks>
        // REVIEW: Should we treat the rule directory as an override (current behavior), or combine it with the Hive directory?
        // REVIEW: Should a default rule definition even be configurable?
        public ConfigRuleProvider(string overrideRuleDirectory, string defaultRuleDefinition, StringView splitToken)
        {
            if (!string.IsNullOrEmpty(overrideRuleDirectory))
            {
                ruleDirectory = overrideRuleDirectory;
            }

            if (!string.IsNullOrEmpty(defaultRuleDefinition))
            {
                this.defaultRuleDefinition = defaultRuleDefinition;
            }

            this.splitToken = splitToken;

            _ = Directory.CreateDirectory(overrideRuleDirectory);
        }

        /// <inheritdoc/>
        public bool HasRuleChangedSince(StringView name, Instant time)
        {
            var fileInfo = TryGetRule(name.ToString()).Data;

            // Refresh access time fields for the file and its parent directory
            fileInfo.Refresh();
            fileInfo.Directory?.Refresh();

            var lastWriteTimeUtc = fileInfo.Exists
                ? fileInfo.LastWriteTimeUtc
                : fileInfo.Directory?.LastWriteTimeUtc;

            return lastWriteTimeUtc is not null && Instant.FromDateTimeUtc(lastWriteTimeUtc.Value) > time;
        }

        /// <inheritdoc/>
        // REVIEW: I made this method call the other HasRuleChangedSince because the code was very similar. Am I going to get punched for this?
        public bool HasRuleChangedSince(Rule rule, Instant time) => rule is null ? throw new ArgumentNullException(nameof(rule)) : HasRuleChangedSince(rule.Name, time);

        /// <inheritdoc/>
        public bool TryGetRule(StringView name, [MaybeNullWhen(false)] out Rule gotten)
        {
            var rule = TryGetRule(name.ToString());

            gotten = rule.Data.Exists ? rule : null;

            return gotten != null;
        }

        // Helper function that obtains cached information about a rule. If none exists, it goes to the file system.
        private Rule<FileInfo> TryGetRule(string name)
        {
            if (cachedFileInfos.TryGetValue(name, out var fileInfo))
            {
                return fileInfo;
            }
            else
            {
                var fromFileSystem = GetFromFileSystem(name, GetRuleLocation(name));
                cachedFileInfos.Add(name, fromFileSystem);
                return fromFileSystem;
            }
        }

        // Helper function that reads information about a rule from the file system.
        private Rule<FileInfo> GetFromFileSystem(string ruleName, string filePath)
        {
            var ruleDefinition = defaultRuleDefinition;

            if (File.Exists(filePath))
            {
                ruleDefinition = File.ReadAllText(filePath);
            }

            var fileInfo = new FileInfo(filePath);
            var rule = new Rule<FileInfo>(ruleName, ruleDefinition, fileInfo);

            return rule;
        }

        // Helper function that returns the file system location for a rule
        private string GetRuleLocation(StringView ruleName)
        {
            var parts = ruleName.Split(splitToken, ignoreEmpty: false);
            var localRuleDirectory = string.Join(@"\", parts);

            return Path.Combine(ruleDirectory, localRuleDirectory, ".rule");
        }
    }
}
