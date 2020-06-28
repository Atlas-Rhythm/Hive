using System;
using System.Collections.Generic;
using System.Text;

namespace Hive.Utilities
{
    public static class Helpers
    {
        public static IEnumerable<T> InterleaveWith<T>(this IEnumerable<T> first, IEnumerable<T> second)
        {
            var a = first.GetEnumerator();
            var b = second.GetEnumerator();

            while (true)
            {
                bool ba, bb;
                if (ba = a.MoveNext()) yield return a.Current;
                if (bb = b.MoveNext()) yield return b.Current;
                if (!ba && !bb) yield break;
            }
        }

        public static IEnumerable<T> Repeat<T>(T val, int count)
        {
            for (int i = 0; i < count; i++)
                yield return val;
        }
    }
}
