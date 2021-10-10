using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Hive.Versioning.Resources;

namespace Hive.Versioning
{
    internal static class StaticHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // when we upgrade to C# 10, we can use CallerArgumentExpression
        public static void Assert([DoesNotReturnIf(false)] bool value, /*[CallerArgumentExpression("value")]*/ string expr = "")
        {
            if (!value)
                throw new InvalidOperationException(SR.AssertionFailed.Format(expr));
        }
    }
}
