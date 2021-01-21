using System;
using Hive.CodeGen;
using Xunit;

namespace Hive.Utilities.Tests
{
    public partial class CodegenGenericMethods
    {
        [Fact]
        public void TestGenericMethod()
        {
            var _1 = ValueTupleOf(1, 2, 3, 4, 5);
            var _2 = ValueTupleOf(1, 2, 3, 4, 5, 6, 7, 8);
            _ = _1;
            _ = _2;
        }

        public static ValueTuple<T> ValueTupleOf<T>(T val)
            => new(val);

        [ParameterizeGenericParameters(2, 9)]
        public static (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) ValueTupleOf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10)
            => (_1, _2, _3, _4, _5, _6, _7, _8, _9, _10);
    }
}
