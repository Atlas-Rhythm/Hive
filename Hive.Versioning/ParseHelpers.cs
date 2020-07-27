using System;
using System.Collections.Generic;
using System.Text;

namespace Hive.Versioning
{
    internal static class ParseHelpers
    {
        public static bool TryTake(ref ReadOnlySpan<char> input, char next)
        {
            if (input.Length == 0) return false;
            if (input[0] != next) return false;
            input = input.Slice(1);
            return true;
        }
    }
}
