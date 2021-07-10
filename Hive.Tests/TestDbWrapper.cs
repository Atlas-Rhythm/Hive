using Hive.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Hive.Tests
{
    /// <summary>
    /// An extendable class that indicates a test is for
    /// </summary>
    public class TestDbWrapper
    {
        /// <summary>
        /// The <see cref="DbContextOptions{TContext}"/> that have been initialized to point to a test DB.
        /// </summary>
        protected DbContextOptions<HiveContext> Options { get; }

        // Avoid inlining so we can get the type name via a stack trace search
        [MethodImpl(MethodImplOptions.NoInlining)]
        public TestDbWrapper(PartialContext? context = null)
        {
            var stackTrace = new StackTrace(1, false);
            // We assert that there is a frame 1 above us and that it has a method, and that method has a declaring type.
            var testId = stackTrace.GetFrame(0)!.GetMethod()!.DeclaringType!.FullName;
            // DB name for the test
            var dbName = "test_" + testId;
            // Get connection string
            var config = new ConfigurationBuilder().AddUserSecrets(typeof(Program).Assembly).Build();
            var connectionString = config.GetConnectionString("Test");
            // Create DB with connection string
            //var outterConnection = new NpgsqlConnection(connectionString);
            //outterConnection.Open();
            //var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS {dbName}; CREATE DATABASE {dbName};", outterConnection);
            //cmd.ExecuteNonQuery();
            //outterConnection.Close();
            // Create Options
            Options = new DbContextOptionsBuilder<HiveContext>().UseNpgsql(connectionString + "Database='" + dbName + "';", o => o.UseNodaTime()).Options;
            // Setup DB using context
            TestHelpers.SetupDb(Options, context);
        }
    }
}
