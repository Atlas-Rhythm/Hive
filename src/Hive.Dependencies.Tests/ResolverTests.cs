using Microsoft.FSharp.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Hive.Versioning;
using Version = Hive.Versioning.Version;

namespace Hive.Dependencies.Tests
{
    public class ResolverTests
    {
        private class Mod
        {
            public string Id { get; set; } = "";
            public Version Version { get; set; } = Version.Zero;
            public List<ModRef> Dependencies { get; set; } = new List<ModRef>();
            public List<ModRef> Conflicts { get; set; } = new List<ModRef>();
        }

        private struct ModRef
        {
            public string Id { get; }
            public VersionRange Range { get; }

            public ModRef(string id, VersionRange vers)
            {
                Id = id;
                Range = vers;
            }
        }

        private class Accessor : IValueAccessor<Mod, ModRef, Version, VersionRange>
        {
            public readonly List<Mod> ExistingMods;

            public Accessor(List<Mod> mods) => ExistingMods = mods;

            public Task<IEnumerable<Mod>> ModsMatching(ModRef @ref)
            {
                return Task.FromResult(ExistingMods.Where(m => m.Id == @ref.Id && @ref.Range.Matches(m.Version)));
            }

            public FSharpValueOption<VersionRange> And(VersionRange a, VersionRange b)
            {
                var res = a & b;
                if (res == VersionRange.Nothing)
                    return FSharpValueOption<VersionRange>.None;
                return FSharpValueOption<VersionRange>.Some(res);
            }

            public int Compare(Version a, Version b) => a.CompareTo(b);

            public IEnumerable<ModRef> Conflicts(Mod mod_) => mod_.Conflicts;

            public ModRef CreateRef(string id, VersionRange range) => new(id, range);

            public IEnumerable<ModRef> Dependencies(Mod mod_) => mod_.Dependencies;

            public VersionRange Either(VersionRange a, VersionRange b) => a | b;

            public string ID(Mod mod_) => mod_.Id;

            public string ID(ModRef @ref) => @ref.Id;

            public bool Matches(VersionRange range, Version version) => range.Matches(version);

            public VersionRange Not(VersionRange a) => ~a;

            public VersionRange Range(ModRef @ref) => @ref.Range;

            public Version Version(Mod mod_) => mod_.Version;
        }

        [Fact]
        public async Task TestResolver()
        {
            var accessor = new Accessor(new List<Mod>()
            {
                new Mod { Id = "BSIPA", Version = new Version("4.0.5") },
                new Mod
                {
                    Id = "SongCore", Version = new Version("2.9.11"),
                    Dependencies = new List<ModRef>
                    {
                        new ModRef("BeatSaberMarkupLanguage", new VersionRange("^1.3.4")),
                        new ModRef("BS Utils", new VersionRange("^1.4.11")),
                        new ModRef("BSIPA", new VersionRange("^4.0.0")),
                    }
                },
                new Mod
                {
                    Id = "ScoreSaber", Version = new Version("2.3.5"),
                    Dependencies = new List<ModRef>
                    {
                        new ModRef("SongCore", new VersionRange("^2.9.10")),
                        new ModRef("BS Utils", new VersionRange("^1.4.10")),
                    }
                },
                new Mod { Id = "Ini Parser", Version = new Version("2.5.7") },
                new Mod
                {
                    Id = "BS Utils", Version = new Version("1.4.11"),
                    Dependencies = new List<ModRef> { new ModRef("Ini Parser", new VersionRange("^2.5.4")) }
                },
                new Mod
                {
                    Id = "BeatSaberMarkupLanguage", Version = new Version("1.3.4"),
                    Dependencies = new List<ModRef> { new ModRef("BS Utils", new VersionRange("^1.4.10")) },
                }
            });

            var res = await Resolver.Resolve(accessor, accessor.ExistingMods.Where(m => m.Id == "ScoreSaber").Take(1));

            var arr = res.ToArray();
        }
    }
}
