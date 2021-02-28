using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Hive.Models;

namespace Hive.Tests
{
    internal class DbExamples
    {
        public IEnumerable<Mod> GenericMods { get; }
        public IEnumerable<Channel> GenericChannels { get; }
        public IDictionary<string, User> GenericUsers { get; }

        public DbExamples()
        {
            GenericUsers = new Dictionary<string, User>
            {
                { "Eris", new User { Username = "Eris", AdditionalData = DIHelper.EmptyAdditionalData } },
                { "Auros", new User { Username = "Auros", AdditionalData = DIHelper.EmptyAdditionalData } },
                { "DaNike", new User { Username = "DaNike", AdditionalData = DIHelper.EmptyAdditionalData } },
                { "Umbranox", new User { Username = "Umbranox", AdditionalData = DIHelper.EmptyAdditionalData } },
                { "Caeden117", new User { Username = "Caeden117", AdditionalData = DIHelper.EmptyAdditionalData } },
                { "monkeymanboy", new User { Username = "monkeymanboy", AdditionalData = DIHelper.EmptyAdditionalData } },
                { "Toni Macaroni", new User { Username = "Toni Macaroni", AdditionalData = DIHelper.EmptyAdditionalData } },
                { "raftario", new User { Username = "raftario best modder", AdditionalData = DIHelper.EmptyAdditionalData } },
            };

            GenericChannels = new List<Channel>
            {
                new Channel { Name = "Beta", AdditionalData = DIHelper.EmptyAdditionalData },
                new Channel { Name = "Denied", AdditionalData = DIHelper.EmptyAdditionalData },
                new Channel { Name = "Public", AdditionalData = DIHelper.EmptyAdditionalData },
                new Channel { Name = "Pending", AdditionalData = DIHelper.EmptyAdditionalData },
            };

            GenericMods = new List<Mod>
            {
                CreateDummyMod("BSIPA", "Public", GenericUsers["DaNike"], new Versioning.Version(4, 2, 0)),
                CreateDummyMod("lilac", "Public", GenericUsers["raftario"], new Versioning.Version(2, 5, 1)),
                CreateDummyMod("SiraUtil", "Public", GenericUsers["Auros"], new Versioning.Version(3, 0, 2)),
                CreateDummyMod("SiraUtil", "Pending", GenericUsers["Auros"], new Versioning.Version(3, 1, 0)),
                CreateDummyMod("ScoreSaber", "Public", GenericUsers["Umbranox"], new Versioning.Version(3, 0, 2)),
                CreateDummyMod("CountersPlus", "Public", GenericUsers["Caeden117"], new Versioning.Version(3, 1, 0)),
                CreateDummyMod("CountersPlus", "Denied", GenericUsers["Caeden117"], new Versioning.Version(3, 1, 1)),
                CreateDummyMod("CountersPlus", "Pending", GenericUsers["Caeden117"], new Versioning.Version(3, 1, 2)),
                CreateDummyMod("HitScoreVisualizer", "Public", GenericUsers["Eris"], new Versioning.Version(2, 5, 1)),
                CreateDummyMod("TrickSaber", "Beta", GenericUsers["Toni Macaroni"], new Versioning.Version(2, 5, 1)),
                CreateDummyMod("SaberFactory", "Public", GenericUsers["Toni Macaroni"], new Versioning.Version(2, 3, 0)),
                CreateDummyMod("BeatSaberMarkupLanguage", "Public", GenericUsers["monkeymanboy"], new Versioning.Version(1, 5, 0)),
            };
        }

        private Mod CreateDummyMod(string name, string channel, User user, Versioning.Version? version = null)
        {
            var mod = new Mod
            {
                ReadableID = name,
                Version = version ?? new Versioning.Version(1, 0, 0),
                UploadedAt = new NodaTime.Instant(),
                Uploader = user,
                Channel = GenericChannels.First(c => c.Name == channel),
                DownloadLink = new Uri("https://github.com/Atlas-Rhythm/Hive"),
                AdditionalData = DIHelper.EmptyAdditionalData
            };

            var info = new LocalizedModInfo()
            {
                OwningMod = mod,
                Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Name = name,
                Description = $"{name}'s description."
            };

            mod.Localizations.Add(info);
            return mod;
        }

        internal static PartialContext PopulatedPartialContext()
        {
            var db = new DbExamples();
            return new PartialContext { Channels = db.GenericChannels, Mods = db.GenericMods };
        }
    }
}
