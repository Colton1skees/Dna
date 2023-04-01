using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Analysis
{
    public class Loop
    {
        public readonly nint Handle;

        /// <summary>
        /// Gets the name of the loop.
        /// </summary>
        public string Name => GetName();

        /// <summary>
        /// Gets the nesting level of the loop.
        /// </summary>
        public unsafe uint Depth => NativeLoopApi.GetLoopDepth(this);

        /// <summary>
        /// Gets the header block of the loop.
        /// </summary>
        public unsafe LLVMBasicBlockRef Header => NativeLoopApi.GetHeader(this);

        /// <summary>
        /// Gets the parent loop if it exists, or null if it is a top level loop.
        /// </summary>
        public Loop? ParentLoop => GetParentLoop();

        /// <summary>
        /// Gets the outermost loop in which this loop is contained.
        /// </summary>
        public Loop? OutermostLoop => GetOutermostLoop();

        /// <summary>
        /// Gets all loops contained entirely within this loop.
        /// </summary>
        public IReadOnlyList<Loop> SubLoops => GetSubLoops();

        /// <summary>
        /// Returns true if the loop does not contain any (natural) loop,
        /// </summary>
        public unsafe bool IsInnermost => NativeLoopApi.IsInnermost(this);

        /// <summary>
        /// Return true if the loop does not have a parent (natural) loop.
        /// </summary>
        public unsafe bool IsOutermost => NativeLoopApi.IsOutermost(this);

        /// <summary>
        /// Gets a list of basic blocks which make up this loop.
        /// </summary>
        public IReadOnlyList<LLVMBasicBlockRef> Blocks => GetBlocks();

        /// <summary>
        /// Calculate the number of back edges to the loop header.
        /// </summary>
        public unsafe int BackEdgeCount => NativeLoopApi.GetNumBackEdges(this);

        /// <summary>
        /// Gets all blocks inside the loop that have successors outside the of the loop.
        /// </summary>
        public IReadOnlyList<LLVMBasicBlockRef> ExitingBlocks => GetLoopExitingBlocks();

        /// <summary>
        /// Get all successor blocks of this loop.
        /// </summary>
        public IReadOnlyList<LLVMBasicBlockRef> ExitBlocks => GetLoopExitBlocks();

        public Loop(nint handle)
        {
            Handle = handle;
        }

        private unsafe string GetName()
        {
            var pStr = NativeLoopApi.GetName(this);
            if (pStr == null)
                return String.Empty;

            var result = SpanExtensions.AsString(pStr);
            LLVM.DisposeMessage(pStr);
            return result;
        }

        private unsafe Loop? GetParentLoop()
        {
            var parentPtr = NativeLoopApi.GetParentLoop(this);
            return parentPtr == null ? null : parentPtr;
        }

        private unsafe Loop? GetOutermostLoop()
        {
            var parentPtr = NativeLoopApi.GetOutermostLoop(this);
            return parentPtr == null ? null : parentPtr;
        }

        /// <summary>
        /// Gets whether the specified loop is contained within this loop.
        /// </summary>
        /// <param name="loop"></param>
        /// <returns></returns>
        public unsafe bool Contains(Loop loop) => NativeLoopApi.ContainsLoop(this, loop);

        /// <summary>
        /// Gets whether the specified basic block is in this loop.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public unsafe bool Contains(LLVMBasicBlockRef block) => NativeLoopApi.ContainsBlock(this, block);

        /// <summary>
        /// Gets whether the specified instruction is in this loop.
        /// </summary>
        /// <param name="instruction"></param>
        /// <returns></returns>
        public unsafe bool Contains(LLVMValueRef instruction) => NativeLoopApi.ContainsInstruction(this, instruction);

        private unsafe IReadOnlyList<Loop> GetSubLoops()
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeLoopApi.GetSubLoops(this);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<Loop>((nint)vecPtr,
                (nint ptr) => new Loop(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        private unsafe IReadOnlyList<LLVMBasicBlockRef> GetBlocks()
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeLoopApi.GetBlocks(this);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMBasicBlockRef>((nint)vecPtr,
                (nint ptr) => new LLVMBasicBlockRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        private unsafe IReadOnlyList<LLVMBasicBlockRef> GetLoopExitingBlocks()
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeLoopApi.GetExitingBlocks(this);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMBasicBlockRef>((nint)vecPtr,
                (nint ptr) => new LLVMBasicBlockRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        private unsafe IReadOnlyList<LLVMBasicBlockRef> GetLoopExitBlocks()
        {
            // Get an unmanaged vector ptr,.
            var vecPtr = NativeLoopApi.GetExitBlocks(this);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMBasicBlockRef>((nint)vecPtr,
                (nint ptr) => new LLVMBasicBlockRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }

        public unsafe bool IsLoopExiting(LLVMBasicBlockRef block)
        {
            return NativeLoopApi.IsLoopExiting(this, block);
        }

        public unsafe static implicit operator LLVMOpaqueLoop*(Loop pass)
        {
            return (LLVMOpaqueLoop*)pass.Handle;
        }

        public unsafe static implicit operator Loop(LLVMOpaqueLoop* pass)
        {
            return new Loop((nint)pass);
        }
    }
}
