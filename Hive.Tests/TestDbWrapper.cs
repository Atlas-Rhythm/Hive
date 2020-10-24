using Hive.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "<Pending>")]
        public TestDbWrapper(string testId, PartialContext? context = null)
        {
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
            DbHelper.SetupDb(Options, context);
        }
    }
}