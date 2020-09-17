using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Hive.CodeGen;

namespace Hive.Utilities
{
    [ParameterizeGenericParameters(1, 7)]
    public struct UnformattedString<T1, T2, T3, T4, T5, T6, T7, TRest>
        where TRest : struct
    {
        private readonly CultureInfo Culture;
        private readonly string FormatString;
        public UnformattedString(CultureInfo culture, string formatString)
            => (Culture, FormatString) = (culture, formatString);

        public string Format(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> args)
            => string.Format(Culture, FormatString, args.ToArray());
    }
}
