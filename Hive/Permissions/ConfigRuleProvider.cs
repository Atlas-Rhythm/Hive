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
        private readonly StringView splitToken = "*";

        // Rule name to information corresponding to the particular rule.
        private readonly Dictionary<string, (FileInfo, Rule)> cachedFileInfos = new();

        /// <summary>
        /// Construct a rule provider via DI.
        /// </summary>
        public ConfigRuleProvider()
        {
            // TODO: Read base directory (and default rule value) from config
        }

        /// <inheritdoc/>
        public bool HasRuleChangedSince(StringView name, Instant time)
        {
            var fileInfo = TryGetRule(name.ToString(), out var newlyCreatedRule).Item1;
            return fileInfo.LastWriteTimeUtc != time.ToDateTimeUtc() || newlyCreatedRule;
        }

        /// <inheritdoc/>
        public bool HasRuleChangedSince(Rule rule, Instant time)
        {
            if (rule is null) throw new ArgumentNullException(nameof(rule));

            var fileInfo = TryGetRule(rule.Name, out var newlyCreatedRule).Item1;
            return fileInfo.LastWriteTimeUtc != time.ToDateTimeUtc() || newlyCreatedRule;
        }

        /// <inheritdoc/>
        public bool TryGetRule(StringView name, [MaybeNullWhen(false)] out Rule gotten)
        {
            gotten = TryGetRule(name.ToString(), out _).Item2;
            return true;
        }

        // Helper function that obtains cached information about a rule. If none exists, it goes to the file system.
        private (FileInfo, Rule) TryGetRule(string name, out bool newlyCreated)
        {
            if (cachedFileInfos.TryGetValue(name, out var fileInfo))
            {
                newlyCreated = false;
                return fileInfo;
            }
            else
            {
                var fromFileSystem = GetOrCreateFromFileSystem(name, GetRuleLocation(name), out newlyCreated);
                cachedFileInfos.Add(name, fromFileSystem);
                return fromFileSystem;
            }
        }

        // Helper function that reads information about a rule from the file system.
        // If the rule does not exist in the file system, we write a new rule with the default definition.
        private (FileInfo, Rule) GetOrCreateFromFileSystem(string ruleName, string filePath, out bool newlyCreated)
        {
            newlyCreated = false;
            var ruleDefinition = defaultRuleDefinition;

            if (File.Exists(filePath))
            {
                ruleDefinition = File.ReadAllText(filePath);
            }
            else
            {
                newlyCreated = true;

                using var writer = File.CreateText(filePath);
                writer.WriteLine(ruleDefinition);
            }

            var fileInfo = new FileInfo(filePath);
            var rule = new Rule(ruleName, ruleDefinition);

            return (fileInfo, rule);
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
