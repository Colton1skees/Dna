using System;
using System.Collections.Generic;
using System.Linq;
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

    public class InstData<TAddress, TMetadata>
        where TAddress : IEquatable<TAddress>
    {
        public HashSet<TAddress> Predecessors { get; } = new(2);

        public HashSet<AnnotatedEdge<TAddress>> Successors { get; } = new(2);

        public TMetadata Metadata { get; set; }
    }

    public class InstGraph<TAddress, TMetadata>
        where TAddress : IEquatable<TAddress>
    {
        public Dictionary<TAddress, InstData<TAddress, TMetadata>> Instructions { get; } = new();
    }
}
