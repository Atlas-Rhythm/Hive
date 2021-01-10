#if !NETSTANDARD2_0
using System;
#else
using Hive.Utilities;
#endif
using System.Runtime.CompilerServices;

namespace Hive.Versioning
{
    internal static class ParseHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if !NETSTANDARD2_0
        public static bool TryTake(ref ReadOnlySpan<char> input, char next)
#else
        public static bool TryTake(ref StringView input, char next)
#endif
        {
            if (input.Length == 0) return false;
            if (input[0] != next) return false;
            input = input.Slice(1);
            return true;
        }
    }
}
