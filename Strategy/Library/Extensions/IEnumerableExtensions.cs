using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Strategy.Library.Extensions
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Performs the specified action on each element of the IEnumerable<T>
        /// </summary>
        /// <param name="enumerable">The IEnumerable<T> to operate on.</param>
        /// <param name="action">The System.Action<T> delegate to perform on each element of the IEnumerable<T></param>
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
    }
}
