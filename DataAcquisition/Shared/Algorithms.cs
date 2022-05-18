using System;
using System.Collections.Generic;

namespace DataAcquisition.Shared
{
    public static class Algorithms
    {
        public static long CountDistinctInSortedSet<T>(IEnumerable<T> set, Comparison<T> comparison)
        {
            using var enumerator = set.GetEnumerator();

            if (!enumerator.MoveNext())
                return 0;

            var value = enumerator.Current;
            long count = 1;
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;

                if (comparison(value, current) == 0)
                {
                    value = current;
                    count++;
                }
            }

            return count;
        }

        public static void ForEachDistinctGroupInSortedArray<T>(T[] set, Comparison<T> comparison,
            Action<int, int> action)
        {
            if (set == null || set.Length == 0)
                return;

            var min = 0;
            do
            {
                var max = min;
                while (max + 1 < set.Length && comparison(set[max], set[max + 1]) == 0) max++;

                action(min, max);
                min = max + 1;
            } while (min < set.Length);
        }
    }
}
