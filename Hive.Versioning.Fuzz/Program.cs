using Hive.Versioning.Tests;
using SharpFuzz;
using System;

namespace Hive.Versioning.Fuzz
{
    class Program
    {
        static void Main()
        {
            var fix = new VersionTestFixture();
            Fuzzer.Run(vertext =>
            {
                var success = Version.TryParse(vertext, out var ver);
                fix.Validate(success, vertext, ver);
            });
        }
    }
}
