using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow.DataStructures
{
    public class OrderedSet<T> : ICollection<T>
    {
        private readonly IDictionary<T, LinkedListNode<T>> dictionary;

        private readonly LinkedList<T> linkedList;

        public OrderedSet()
            : this(EqualityComparer<T>.Default)
        {
        }

        public OrderedSet(IEnumerable<T> input)
        {
            var comparer = EqualityComparer<T>.Default;
            dictionary = new Dictionary<T, LinkedListNode<T>>(comparer);
            linkedList = new LinkedList<T>();
            foreach (var item in input)
            {
                Add(item);
            }
        }
           

        public OrderedSet(IEqualityComparer<T> comparer)
        {
            dictionary = new Dictionary<T, LinkedListNode<T>>(comparer);
            linkedList = new LinkedList<T>();
        }

        public int Count
        {
            get { return dictionary.Count; }
        }

        public virtual bool IsReadOnly
        {
            get { return dictionary.IsReadOnly; }
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public void Clear()
        {
            linkedList.Clear();
            dictionary.Clear();
        }

        public bool Remove(T item)
        {
            LinkedListNode<T> node;
            bool found = dictionary.TryGetValue(item, out node);
            if (!found)
                return false;
            dictionary.Remove(item);
            linkedList.Remove(node);
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return linkedList.GetEnumerator();
        } 

        public bool Contains(T item)
        {
            return dictionary.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            linkedList.CopyTo(array, arrayIndex);
        }

        public bool Add(T item)
        {
            if (dictionary.ContainsKey(item))
                return false;
            LinkedListNode<T> node = linkedList.AddLast(item);
            dictionary.Add(item, node);
            return true;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return linkedList.GetEnumerator();
        }
    }
}
