using Dna.LLVMInterop.API.RegionAnalysis.Native;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.RegionAnalysis.Wrapper
{
    public unsafe abstract class Region : IEquatable<Region>
    {
        protected readonly nint handle;

        public RegionKind Kind => NativeRegionApi.RegionGetKind(handle);

        public ComplexRegion? Owner
        {
            get
            {
                var regionPtr = NativeRegionApi.RegionGetOwnerRegion(handle);
                return regionPtr == nint.Zero ? null : (ComplexRegion)CreateRegion(regionPtr);
            }
        }

        public Region EntryRegion => NativeRegionApi.RegionGetEntryRegionBlock(handle);

        public ulong Id => NativeRegionApi.RegionGetId(handle);

        public ulong PredecessorCount => NativeRegionApi.RegionGetPredCount(handle);

        public IEnumerable<Region> Predecessors => GetPredecessors();

        public ulong SuccessorCount => NativeRegionApi.RegionGetPredCount(handle);

        public IEnumerable<Region> Successors => GetSuccessors();

        public ulong ChildCount => NativeRegionApi.RegionGetChildCount(handle);

        public IEnumerable<Region> Children => GetChildren();

        public LLVMBasicBlockRef BasicBlock => new LLVMBasicBlockRef(NativeRegionApi.RegionGetLLVMBasicBlock(handle));

        public LLVMBasicBlockRef HeadBasicBlock => new LLVMBasicBlockRef(NativeRegionApi.RegionGetHeadLLVMBasicBlock(handle));

        public Region(nint handle)
        {
            this.handle = handle;
        }

        private IEnumerable<Region> GetPredecessors()
        {
            for (ulong i = 0; i < PredecessorCount; i++)
            {
                yield return GetPredecessor(i);
            }

            yield break;
        }

        public Region GetPredecessor(ulong index)
        {
            return NativeRegionApi.RegionGetPred(handle, index);
        }

        private IEnumerable<Region> GetSuccessors()
        {
            for (ulong i = 0; i < SuccessorCount; i++)
            {
                yield return GetSuccessor(i);
            }

            yield break;
        }

        public Region GetSuccessor(ulong index)
        {
            return NativeRegionApi.RegionGetSucc(handle, index);
        }

        private IEnumerable<Region> GetChildren()
        {
            for (ulong i = 0; i < ChildCount; i++)
            {
                yield return GetChild(i);
            }

            yield break;
        }

        public Region GetChild(ulong index)
        {
            return NativeRegionApi.RegionGetChild(handle, index);
        }

        public static Region CreateRegion(nint ptr)
        {
            var kind = NativeRegionApi.RegionGetKind(ptr);

            return kind switch
            {
                RegionKind.Block => new BlockRegion(ptr),
                RegionKind.Sequence => new SequenceRegion(ptr),
                RegionKind.IfThen => new IfThenRegion(ptr),
                RegionKind.IfThenElse => new IfThenElseRegion(ptr),
                RegionKind.NaturalLoop => new NaturalLoopRegion(ptr),
                RegionKind.Break => new BreakRegion(ptr),
                RegionKind.Continue => new ContinueRegion(ptr),
                RegionKind.Return => new ReturnRegion(ptr),
                RegionKind.Switch => new SwitchRegion(ptr),
                _ => throw new InvalidOperationException($"Region kind {kind} is not supported."),
            };
        }

        public override bool Equals(object? obj) => obj is Region other && Equals(other);

        public bool Equals(Region other) => this == other;

        public override int GetHashCode() => handle.GetHashCode();

        public static bool operator ==(Region left, Region right) => left.handle == right.handle;

        public static bool operator !=(Region left, Region right) => !(left == right);

        public static implicit operator Region(nint value) => CreateRegion(value);
    }
}
