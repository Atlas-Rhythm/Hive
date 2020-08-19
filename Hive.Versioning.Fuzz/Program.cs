using Hive.Versioning.Tests;
using SharpFuzz;
using System;
using System.Linq;

namespace Hive.Versioning.Fuzz
{
    class Program
    {
        static int Main(string[] args)
        {
            var type = args.Length > 0 ? args[0] : "range";

            if (type == "version")
            {
                var fix = new VersionTestFixture();
                Fuzzer.Run(vertext =>
                {
                    vertext = vertext.Trim();
                    var success = Version.TryParse(vertext, out var ver);
                    fix.Validate(success, vertext, ver);
                });
            }
            else if (type == "range")
            {
                Fuzzer.Run(rangetext =>
                {
                    rangetext = rangetext.Trim();
                    if (VersionRange.TryParse(rangetext, out var range))
                    {
                        if (args.Contains("disj"))
                        {
                            range |= range;
                            range |= range;
                            range |= range;
                            range |= range;
                        }
                        _ = range.ToString();
                    }
                });
            }
            else
            {
                return 1;
            }

            return 0;
        }
    }
}
