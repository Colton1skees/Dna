using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.DataStructures
{
    public class WorkList<T> : IEnumerable<T>
    {
        private readonly HashSet<T> _inserted;
        private readonly Deque<T> _deque;

        public int Count
        {
            get { return _deque.Count; }
        }

        public WorkList()
            : this(EqualityComparer<T>.Default)
        {
        }

        public WorkList(IEnumerable<T> input)
            : this(EqualityComparer<T>.Default)
        {
            foreach (var item in input)
            {
                AddToBack(item);
            }
        }
           
        public WorkList(IEqualityComparer<T> comparer)
        {
            _inserted = new(comparer);
            _deque = new();
        }

        public void Clear()
        {
            _deque.Clear();
            _inserted.Clear();
        }

        public IEnumerator<T> GetEnumerator() => _deque.GetEnumerator();

        public bool Contains(T item) => _inserted.Contains(item);

        public bool AddToFront(T item)
        {
            if (_inserted.Contains(item))
                return false;

            _deque.AddToFront(item);
            _inserted.Add(item);
            return true;
        }

        public bool AddToBack(T item)
        {
            if (_inserted.Contains(item))
                return false;

            _deque.AddToBack(item);
            _inserted.Add(item);
            return true;
        }

        public void AddRangeToFront(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                AddToFront(item);
            }
        }

        public void AddRangeToBack(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                AddToBack(item);
            }
        }

        public T PopFront()
        {
            var v = _deque.RemoveFromFront();
            _inserted.Remove(v);
            return v;
        }

        public T PopBack()
        {
            var v = _deque.RemoveFromBack();
            _inserted.Remove(v);
            return v;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _deque.GetEnumerator();
    }
}
