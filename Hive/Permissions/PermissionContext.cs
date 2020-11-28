using Hive.Models;

namespace Hive.Permissions
{
    /// <summary>
    /// The context used for permissions checks.
    /// </summary>
    /// <seealso cref="PermissionsManager{TContext}"/>
    public class PermissionContext
    {
        /// <summary>
        /// The currently logged in <see cref="Models.User"/>. <c>null</c> if the user is not logged in.
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// The <see cref="Models.Mod"/> to check access to. <c>null</c> if there is no mod in this context.
        /// </summary>
        public Mod? Mod { get; set; }

        /// <summary>
        /// The <see cref="Models.Channel"/> to check access to. <c>null</c> if there is no channel in this context.
        /// </summary>
        public Channel? Channel { get; set; }

        /// <summary>
        /// The <see cref="Models.GameVersion"/> to check access to. <c>null</c> if there is no game version in this context.
        /// </summary>
        public GameVersion? GameVersion { get; set; }

        /// <summary>
        /// The <see cref="Models.Channel"/> source for a move request. Null if this is not a move request.
        /// </summary>
        /// <seealso cref="DestinationChannel"/>
        public Channel? SourceChannel { get; set; }

        /// <summary>
        /// The <see cref="Models.Channel"/> destination for a move request. Null if this is not a move request.
        /// </summary>
        /// <seealso cref="SourceChannel"/>
        public Channel? DestinationChannel { get; set; }
    }
}