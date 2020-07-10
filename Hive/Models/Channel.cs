using System;
using System.Text.Json;

namespace Hive.Models
{
    public class Channel
    {
        // this could also act as a primary key
        public string Name { get; set; } = null!;

        // like Mod's
        public JsonElement AdditionalData { get; set; }

        public bool IsPublic { get; set; } = true;

        // TODO: is there anything else that needs to be on any particular channel object?
    }
}