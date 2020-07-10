using SemVer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Version = SemVer.Version;
using VerRange = SemVer.Range;

namespace Hive.Models
{
    public class Mod
    {
        public string ID { get; } = null!;

        // one to many
        public List<LocalizedModInfo> Localizations { get; } = new List<LocalizedModInfo>();

        // this would ideally be a SemVer version object from somewhere
        public Version Version { get; } = null!;

        // many to one
        public User Uploader { get; } = null!;

        // many to many
        public List<User> Authors { get; } = new List<User>();

        // many to many
        public List<User> Contributors { get; } = new List<User>();

        // many to many (this needs to use a join type, and needs modification to be put into EF)
        public List<GameVersion> SupportedVersions { get; } = new List<GameVersion>();

        public List<ModReference> Dependencies { get; } = new List<ModReference>();

        public List<ModReference> Conflicts { get; } = new List<ModReference>();

        // many to one
        public Channel Channel { get; } = null!;

        // this would be a JSON string, encoding arbitrary data (this should be some type that better represents that JSON data though)
        public string? AdditionalData { get; }

        public List<(string Name, Uri Url)> Links { get; } = new List<(string, Uri)>();

        public Uri DownloadLink { get; } = null!;

        #region DB Schema stuff
        // this would be the primary key for this row
        public Guid Guid { get; }
        #endregion
    }

    public struct ModReference
    {
        public string ModID { get; }

        public VerRange Versions { get; }
    }
}
