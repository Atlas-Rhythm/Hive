using Microsoft.EntityFrameworkCore;

namespace Hive.Models
{
    /// <summary>
    /// The database context for Hive.
    /// </summary>
    public class HiveContext : DbContext
    {
        /// <summary>
        /// Create a <see cref="DbContext"/> with the provided options.
        /// </summary>
        /// <param name="options">The options to create the context with.</param>
        public HiveContext(DbContextOptions<HiveContext> options) : base(options)
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public HiveContext() : base()
        {
        }

        /// <summary>
        /// The database collection of <see cref="Mod"/> objects.
        /// </summary>
        public virtual DbSet<Mod> Mods { get; set; } = null!;

        /// <summary>
        /// The database collection of <see cref="LocalizedModInfo"/> objects.
        /// </summary>
        public virtual DbSet<LocalizedModInfo> ModLocalizations { get; set; } = null!;

        /// <summary>
        /// The database collection of <see cref="Channel"/> objects.
        /// </summary>
        public virtual DbSet<Channel> Channels { get; set; } = null!;

        /// <summary>
        /// The database collection of <see cref="GameVersion"/> objects.
        /// </summary>
        public virtual DbSet<GameVersion> GameVersions { get; set; } = null!;

        /// <summary>
        /// The database collection of <see cref="User"/> objects.
        /// </summary>
        public virtual DbSet<User> Users { get; set; } = null!;

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            Mod.Configure(modelBuilder);
            GameVersion.Configure(modelBuilder);
        }
    }
}
