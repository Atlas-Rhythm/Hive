using Hive.Models;
using System.Collections.Generic;
using System.Linq;
using Hive.Utilities;

namespace Hive.Tests
{
    /// <summary>
    /// Represents a partial implementation of <see cref="HiveContext"/> that is used for testing.
    /// </summary>
    public sealed class PartialContext
    {
        public IEnumerable<User> Users { get; set; } = null!;
        public IEnumerable<Mod> Mods { get; set; } = null!;
        public IEnumerable<LocalizedModInfo> ModLocalizations { get; set; } = null!;
        public IEnumerable<Channel> Channels { get; set; } = null!;
        public IEnumerable<GameVersion> GameVersions { get; set; } = null!;

        public IEnumerable<User> AllUsers
            => (Users ?? Enumerable.Empty<User>())
            .Concat((Mods ?? Enumerable.Empty<Mod>()).SelectMany(m =>
                new[] { m.Uploader }.Concat(m.Authors).Concat(m.Contributors)))
            .WhereNotNull();
    }
}
