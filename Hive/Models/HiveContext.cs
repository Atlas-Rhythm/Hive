﻿using Microsoft.EntityFrameworkCore;

namespace Hive.Models
{
    public class HiveContext : DbContext
    {
        public HiveContext(DbContextOptions<HiveContext> options) : base(options)
        {
        }

        public HiveContext() : base()
        {
        }

        public virtual DbSet<Mod> Mods { get; set; } = null!;
        public virtual DbSet<LocalizedModInfo> ModLocalizations { get; set; } = null!;
        public virtual DbSet<Channel> Channels { get; set; } = null!;
        public virtual DbSet<GameVersion> GameVersions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            Mod.Configure(modelBuilder);
            GameVersion.Configure(modelBuilder);
        }
    }
}
