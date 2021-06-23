using System.IO;
using Hive.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Hive.Tests
{
    internal static class TestHelpers
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

        internal static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        internal static HttpContext CreateMockRequest(Stream body)
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderNames.Authorization, new StringValues("Bearer: test"));
            context.Request.Body = body;

            return context;
        }
    }
}
