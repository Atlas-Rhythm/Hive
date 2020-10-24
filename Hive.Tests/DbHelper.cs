using Hive.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hive.Tests
{
    internal static class DbHelper
    {
        /// <summary>
        /// Copies data from <see cref="PartialContext"/> to <see cref="HiveContext"/>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="dbContext"></param>
        internal static void CopyData(PartialContext context, HiveContext dbContext)
        {
            if (context.Channels is not null)
                dbContext.Channels.AddRange(context.Channels);
            if (context.GameVersions is not null)
                dbContext.GameVersions.AddRange(context.GameVersions);
            if (context.ModLocalizations is not null)
                dbContext.ModLocalizations.AddRange(context.ModLocalizations);
            if (context.Mods is not null)
                dbContext.Mods.AddRange(context.Mods);
            dbContext.SaveChanges();
        }

        /// <summary>
        /// Sets up the Database. Should only be called once per test (in the test constructor).
        /// </summary>
        /// <param name="options"></param>
        internal static void SetupDb(DbContextOptions<HiveContext> options, PartialContext? initialData = null)
        {
            using var dbContext = new HiveContext(options);
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            if (initialData is not null)
            {
                CopyData(initialData, dbContext);
                dbContext.SaveChanges();
            }
        }
    }
}