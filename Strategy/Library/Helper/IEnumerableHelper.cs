using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Strategy.Library.Helper
{
    public static class IEnumerableHelper
    {
        /// <summary>
        /// Returns an IEnumerable containing a single element.
        /// </summary>
        public static IEnumerable<T> Single<T>(T t)
        {
            yield return t;
        }
    }
}
