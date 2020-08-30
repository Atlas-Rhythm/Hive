﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Models
{
    public class ModsContext : DbContext
    {
        public ModsContext(DbContextOptions<ModsContext> options) : base(options)
        {

        }

        public DbSet<Mod> Mods { get; set; } = null!;

        public DbSet<LocalizedModInfo> ModLocalizations { get; set; } = null!;

        public DbSet<Channel> Channels { get; set; } = null!;

        public DbSet<GameVersion> GameVersions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            Mod.Configure(modelBuilder);
            LocalizedModInfo.Configure(modelBuilder);
            GameVersion.Configure(modelBuilder);
            Channel.Configure(modelBuilder);
        }
    }
}