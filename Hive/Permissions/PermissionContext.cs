using Hive.Models;

namespace Hive.Permissions
{
    /// <summary>
    /// The context for all permission related actions.
    /// </summary>
    public class PermissionContext
    {
        /// <summary>
        /// The <see cref="Models.User"/> of the action being requested (if it exists)
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// The <see cref="Models.Mod"/> of the action being requested (if it exists)
        /// </summary>
        public Mod? Mod { get; set; }

        /// <summary>
        /// The <see cref="Models.Channel"/> of the action being requested (if it exists)
        /// </summary>
        public Channel? Channel { get; set; }

        /// <summary>
        /// The <see cref="Models.GameVersion"/> of the action being requested (if it exists)
        /// </summary>
        public GameVersion? GameVersion { get; set; }

        /// <summary>
        /// The source <see cref="Models.Channel"/> of a move action, if this is a move action.
        /// </summary>
        public Channel? SourceChannel { get; set; }

        /// <summary>
        /// The destination <see cref="Models.Channel"/> of a move action, if this is a move action.
        /// </summary>
        public Channel? DestinationChannel { get; set; }
    }
}
