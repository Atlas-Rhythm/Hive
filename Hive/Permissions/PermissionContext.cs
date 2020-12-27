using Hive.Models;

namespace Hive.Permissions
{
    public class PermissionContext
    {
        public User? User { get; set; }
        public Mod? Mod { get; set; }
        public Channel? Channel { get; set; }
        public GameVersion? GameVersion { get; set; }

        // Used for moving mods between channels
        public Channel? SourceChannel { get; set; }

        public Channel? DestinationChannel { get; set; }
    }
}
