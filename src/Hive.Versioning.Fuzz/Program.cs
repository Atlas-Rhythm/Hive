using Hive.Versioning.Tests;
using SharpFuzz;
using System;

namespace Hive.Versioning.Fuzz
{
    public sealed class Program
    {
        public static int Main(string[] args)
        {
            var type = args?.Length > 0 ? args[0] : "binops";

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
                        _ = range.ToString();
                    }
                });
            }
            else if (type == "binops")
            {
                Fuzzer./*OutOfProcess.*/Run(rangetext =>
                {
                    rangetext = rangetext.Trim();
                    var parts = rangetext.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length != 2) return;
                    var a = parts[0];
                    var b = parts[1];

                    var ver = new Version("1.0.0");
                    var asucc = VersionRange.TryParse(a, out var ar);
                    var bsucc = VersionRange.TryParse(b, out var br);

                    if (asucc && bsucc)
                    {
                        var c = ar! & br!;
                        var d = ar! | br!;
                        _ = c.ToString();
                        _ = d.ToString();
                    }
                    if (asucc)
                    {
                        var e = ~ar!;
                        _ = e.ToString();
                        _ = ar!.Matches(ver);
                    }
                    if (bsucc)
                    {
                        var f = ~br!;
                        _ = f.ToString();
                        _ = br!.Matches(ver);
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
