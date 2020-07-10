using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Hive.Models
{
    public class GameVersion
    {
        public string Name { get; set; } = null!;

        // like Mod's
        public JsonElement AdditionalData { get; set; }

        public List<Mod> SupportedMods { get; set; } = new List<Mod>();

        #region DB Schema stuff
        // this would be the primary key for this row
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }
        #endregion
    }

    internal class GameVersion_Mod_Joiner
    {
        public Mod Mod { get; } = null!;
        public GameVersion Version { get; } = null!;
    }
}