using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Strategy.Library.Extensions
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Performs the specified action on each element of the IEnumerable<T>.
        /// </summary>
        /// <param name="enumerable">The IEnumerable<T> to operate on.</param>
        /// <param name="action">The System.Action<T> delegate to perform on each element of the IEnumerable<T>.</param>
        /// <returns>The IEnumerable<T> iterated over.</returns>
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (T t in enumerable)
            {
                if (t != null)
                {
                    action(t);
                }
            }
            return enumerable;
        }

        /// <summary>
        /// Finds the first index in a sequence of values where a predicate is true.
        /// </summary>
        /// <param name="enumerable">The IEnumerable<T> to operate on.</param>
        /// <param name="action">A function to test each element for a condition.</param>
        /// <returns>The first index where the predicate returns true; otherwise, -1.</returns>
        public static int IndexOf<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            int index = 0;
            foreach (T t in enumerable)
            {
                if (predicate(t))
                {
                    return index;
                }
                index += 1;
            }
            return -1;
        }
    }
}
