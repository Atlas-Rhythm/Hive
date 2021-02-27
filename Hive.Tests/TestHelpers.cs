using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using Hive.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;

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

        internal static HttpContext CreateMockRequest(Stream body, bool loggedIn = true)
        {
            var requestMoq = new Mock<HttpRequest>();
            var claimsMoq = new Mock<ClaimsPrincipal>();

            requestMoq.SetupGet(r => r.Body).Returns(body);

            if (loggedIn)
            {
                // Setup headers to return the bearer token to our test user
                requestMoq.SetupGet(r => r.Headers).Returns(new HeaderDictionary(
                    new Dictionary<string, StringValues>()
                    {
                        { HeaderNames.Authorization, new StringValues("Bearer: test") }
                    })
                );

                // Setup claims principal to return our logged-in user
                claimsMoq.SetupGet(m => m.Identity).Returns(new User { Username = "test" });
            }

            var contextMoq = new Mock<HttpContext>();
            contextMoq.SetupGet(c => c.Request).Returns(requestMoq.Object);
            contextMoq.SetupGet(c => c.User).Returns(claimsMoq.Object);

            return contextMoq.Object;
        }
    }
}
