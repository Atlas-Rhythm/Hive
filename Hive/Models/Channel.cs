using System;

namespace Hive.Models
{
    public class Channel
    {
        // this could also act as a primary key
        public string Name { get; } = null!;

        // like Mod's
        public string? AdditionalData { get; }

        public bool IsPublic { get; } = true;

        // TODO: is there anything else that needs to be on any particular channel object?
    }
}