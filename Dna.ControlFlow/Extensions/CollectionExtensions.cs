using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow.Extensions
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> collection, params T[] items) => AddRange(collection, items);

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach(var item in items)
                collection.Add(item);
        }
    }
}
