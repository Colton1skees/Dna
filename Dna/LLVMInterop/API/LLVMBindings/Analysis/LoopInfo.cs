using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Analysis
{
    public class LoopInfo
    {
        public readonly nint Handle;

        public IReadOnlyList<Loop> LoopsInPreorder => GetLoopsInPreorder();

        public IReadOnlyList<Loop> LoopsInReverseSiblingPreorder => GetLoopsInReverseSiblingPreorder();

        public IReadOnlyList<Loop> TopLevelLoops => GetTopLevelLoops();

        public LoopInfo(nint handle)
        {
            this.Handle = handle;
        }

        private unsafe IReadOnlyList<Loop> GetLoopsInPreorder()
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeLoopInfoApi.GetLoopsInPreOrder(this);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<Loop>((nint)vecPtr,
                (nint ptr) => new Loop(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        private unsafe IReadOnlyList<Loop> GetLoopsInReverseSiblingPreorder()
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeLoopInfoApi.GetLoopsInReverseSiblingPreorder(this);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<Loop>((nint)vecPtr,
                (nint ptr) => new Loop(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public unsafe Loop GetLoopFor(LLVMBasicBlockRef block) => NativeLoopInfoApi.GetLoopFor(this, block);

        public unsafe uint GetLoopDepth(LLVMBasicBlockRef block) => NativeLoopInfoApi.GetLoopDepth(this, block);

        public unsafe bool IsLoopHeader(LLVMBasicBlockRef block) => NativeLoopInfoApi.IsLoopHeader(this, block);

        private unsafe IReadOnlyList<Loop> GetTopLevelLoops()
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeLoopInfoApi.GetTopLevelLoops(this);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<Loop>((nint)vecPtr,
                (nint ptr) => new Loop(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public unsafe static implicit operator LLVMOpaqueLoopInfo*(LoopInfo pass)
        {
            return (LLVMOpaqueLoopInfo*)pass.Handle;
        }

        public unsafe static implicit operator LoopInfo(LLVMOpaqueLoopInfo* pass)
        {
            return new LoopInfo((nint)pass);
        }
    }
}
