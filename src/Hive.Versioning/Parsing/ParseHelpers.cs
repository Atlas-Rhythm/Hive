using System.Runtime.CompilerServices;

#if !NETSTANDARD2_0
using StringPart = System.ReadOnlySpan<char>;
#else
using StringPart = Hive.Utilities.StringView;
#endif

namespace Hive.Versioning.Parsing
{
    internal static class ParseHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryTake(ref StringPart input, char next)
        {
            if (input.Length == 0) return false;
            if (input[0] != next) return false;
            input = input.Slice(1);
            return true;
        }
    }
}
