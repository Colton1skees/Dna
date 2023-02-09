using System;
using System.Collections;
using System.Collections.Generic;

namespace Dna.DataStructures
{
    internal static class CollectionHelpers
    {
        public static IReadOnlyCollection<T> ReifyCollection<T>(IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var result = source as IReadOnlyCollection<T>;
            if (result != null)
                return result;

            var collection = source as ICollection<T>;
            if (collection != null)
                return new CollectionWrapper<T>(collection);

            var nongenericCollection = source as ICollection;
            return nongenericCollection != null 
                ? new NongenericCollectionWrapper<T>(nongenericCollection) : new List<T>(source);
        }

        private sealed class NongenericCollectionWrapper<T> : IReadOnlyCollection<T>
        {
            private readonly ICollection collection;

            public int Count => collection.Count;

            public NongenericCollectionWrapper(ICollection collection)
            {
                if (collection == null)
                    throw new ArgumentNullException(nameof(collection));
                this.collection = collection;
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (T item in collection)
                    yield return item;
            }

            IEnumerator IEnumerable.GetEnumerator() => collection.GetEnumerator();
        }

        private sealed class CollectionWrapper<T> : IReadOnlyCollection<T>
        {
            private readonly ICollection<T> collection;

            public int Count => collection.Count;

            public CollectionWrapper(ICollection<T> collection)
            {
                if (collection == null)
                    throw new ArgumentNullException(nameof(collection));
                this.collection = collection;
            }

            public IEnumerator<T> GetEnumerator() => collection.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => collection.GetEnumerator();
        }
    }
}