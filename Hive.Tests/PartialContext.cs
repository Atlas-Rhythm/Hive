using Hive.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hive.Tests
{
    /// <summary>
    /// Represents a partial implementation of <see cref="HiveContext"/> that is used for testing.
    /// </summary>
    public sealed class PartialContext
    {
        public IEnumerable<Mod> Mods { get; set; } = null!;
        public IEnumerable<LocalizedModInfo> ModLocalizations { get; set; } = null!;
        public IEnumerable<Channel> Channels { get; set; } = null!;
        public IEnumerable<GameVersion> GameVersions { get; set; } = null!;
    }
}