using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Dna.ControlFlow
{
    public enum EdgeKind
    {
        Fallthrough = 0,
        Branch = 1,
    }

    public struct AnnotatedEdge<TAddress> : IEquatable<AnnotatedEdge<TAddress>>
        where TAddress : IEquatable<TAddress>
    {
        public TAddress Address;
        public EdgeKind Kind;

        public bool Equals(AnnotatedEdge<TAddress> other)
            => Kind == other.Kind && EqualityComparer<TAddress>.Default.Equals(Address, other.Address);

        public override bool Equals(object? obj)
            => obj is AnnotatedEdge<TAddress> other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Address, Kind);

        public static bool operator ==(AnnotatedEdge<TAddress> left, AnnotatedEdge<TAddress> right) => left.Equals(right);
        public static bool operator !=(AnnotatedEdge<TAddress> left, AnnotatedEdge<TAddress> right) => !left.Equals(right);
    }

    public class SmallSet<T> : IEnumerable<T>
    {
        public HashSet<T> set;

        public T edge0;

        public T edge1;

        public int count;

        public bool IsSet => set != null;

        public int Count => IsSet ? set.Count : count;

        public Enumerator GetEnumerator() => new(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(T value)
        {
            if (IsSet)
            {
                set.Add(value);
                return;
            }

            if (Contains(value))
                return;

            if (count == 0)
            {
                edge0 = value;
                count = 1;
                return;
            }

            edge1 = value;
            count = 2;
            return;
        }

        public bool Contains(T value)
        {
            if (IsSet)
                return set.Contains(value);

            if (count == 0)
                return false;

            var comparer = EqualityComparer<T>.Default;
            return comparer.Equals(edge0, value) || (count == 2 && comparer.Equals(edge1, value));
        }

        public bool SetEquals(SmallSet<T> other)
        {
            var isSet = IsSet;

            if (isSet != other.IsSet)
                return false;

            if (isSet)
                return set.SetEquals(other.set);

            if (count != other.count)
                return false;

            if (count == 0)
                return true;

            var comparer = EqualityComparer<T>.Default;

            if (count == 1)
                return comparer.Equals(edge0, other.edge0);

            // count == 2
            return
                (comparer.Equals(edge0, other.edge0) &&
                 comparer.Equals(edge1, other.edge1))
                ||
                (comparer.Equals(edge0, other.edge1) &&
                 comparer.Equals(edge1, other.edge0));
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly bool isSet;

            private readonly T edge0;

            private readonly T edge1;

            private readonly int count;

            private HashSet<T>.Enumerator setEnumerator;

            private int index;

            internal Enumerator(SmallSet<T> values)
            {
                isSet = values.IsSet;
                edge0 = values.edge0;
                edge1 = values.edge1;
                count = values.count;
                setEnumerator = isSet ? values.set.GetEnumerator() : default;
                index = -1;
            }

            public T Current => isSet ? setEnumerator.Current : index == 0 ? edge0 : edge1;

            object System.Collections.IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (isSet)
                    return setEnumerator.MoveNext();

                index++;
                return index < count;
            }

            public void Reset() => throw new NotSupportedException();

            public void Dispose() => setEnumerator.Dispose();
        }
    }

    public class InstData<TAddress, TMetadata>
        where TAddress : IEquatable<TAddress>
    {
        public TAddress Address { get; }

        public SmallSet<TAddress> Predecessors { get; set; } = new();

        public SmallSet<TAddress> Successors { get; set; } = new();

        public TMetadata Metadata;

        public InstData(TAddress address)
        {
            Address = address;
        }
    }

    public class InstGraph<TAddress, TMetadata>
        where TAddress : IEquatable<TAddress>
    {
        public Dictionary<TAddress, InstData<TAddress, TMetadata>> Instructions { get; } = new();

        public bool Contains(TAddress address) => Instructions.ContainsKey(address);

        // Adds or gets the instruction at the provided address
        public InstData<TAddress, TMetadata> GetOrAdd(TAddress address)
        {
            if (Instructions.TryGetValue(address, out var existing))
                return existing;

            existing = new(address);
            Instructions.Add(address, existing);
            return existing;
        }

        public void AddEdge(TAddress from, TAddress to)
            => AddEdge(GetOrAdd(from), GetOrAdd(to));

        public void AddEdge(InstData<TAddress, TMetadata> from, InstData<TAddress, TMetadata> to)
        {
            from.Successors.Add(to.Address);
            to.Predecessors.Add(from.Address);
        }

        public InstGraph<TAddress, TMetadata> Clone(Func<TAddress, TAddress> cloneAddress, Func<TMetadata, TMetadata> cloneMetadata)
        {
            var result = new InstGraph<TAddress, TMetadata>();
            foreach (var (currAddress, currData) in Instructions)
            {
                var data = new InstData<TAddress, TMetadata>(currAddress);

                Copy(currData.Predecessors, data.Predecessors);
                Copy(currData.Successors, data.Successors);

                data.Metadata = cloneMetadata(currData.Metadata);
                result.Instructions[currAddress] = data;
            }

            return result;
        }

        private static void Copy<T>(SmallSet<T> from, SmallSet<T> to)
        {
            if (from.IsSet)
            {
                to.set = from.set.ToHashSet();
                return;
            }

            to.count = from.count;
            to.edge0 = from.edge0;
            to.edge1 = from.edge1;
        }
    }
}
