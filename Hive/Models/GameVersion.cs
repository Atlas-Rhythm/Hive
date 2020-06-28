using System;

namespace Hive.Models
{
    public class GameVersion
    {
        public string Name { get; } = null!;

        // like Mod's
        public string? AdditionalData { get; }

        #region DB Schema stuff
        // this would be the primary key for this row
        public Guid Guid { get; }
        #endregion
    }
}