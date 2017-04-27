using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleLauncher.Processes
{
    public static class CollectionExtentions
    {
        public static TimeSpan Sum<T>(this IEnumerable<T> collection, Func<T, TimeSpan> func)
        {
            TimeSpan result = TimeSpan.Zero;
            foreach (T item in collection)
            {
                result.Add(func(item));
            }

            return result;
        }
    }
}
