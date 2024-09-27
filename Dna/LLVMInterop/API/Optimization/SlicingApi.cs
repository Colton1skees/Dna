using Dna.Extensions;
using Dna.LLVMInterop.Souper;
using Dna.LLVMInterop.Souper.Inst;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Optimization
{
    public static class SlicingApi
    {
        public static unsafe void SliceValue(LLVMValueRef function, LLVMValueRef value) => NativeSlicingApi.SliceValue(function, value);

        public static unsafe void Solve(LLVMValueRef function, LLVMValueRef remillJumpCall, LLVMValueRef value, LLVMBasicBlockRef jmpFromBlock, nint lazyValueInfo, nint trySolveConstant) => NativeSlicingApi.Solve(function, remillJumpCall, value, jmpFromBlock, lazyValueInfo, trySolveConstant);

        public static unsafe (IReadOnlySet<ulong> results, bool success) TrySolveAllValues(SouperInstContext ctx, IReadOnlyList<SouperBlockPCMapping> bpcs, IReadOnlyList<SouperInstMapping> pcs,
            SouperInstMapping mapping, SouperInst precondition, bool negate = false, bool dropUB = false, nint trySolveConstant = 0, int maxSolutions = 256)
        {
            var unmanagedBpcs = ManagedVector<nint>.From(bpcs.Select(x => x.handle).ToArray(), x => x);
            var unmanagedPcs = ManagedVector<nint>.From(pcs.Select(x => x.handle).ToArray(), x => x);

            var bpcsPtr = (OpaqueManagedVector<SouperOpaqueBlockPCMapping>*)unmanagedBpcs.Handle;
            var pcsPtr = (OpaqueManagedVector<SouperOpaqueInstMapping>*)unmanagedPcs.Handle;

            bool success;
            Console.WriteLine($"Solving!");
            var vecPtr = NativeSlicingApi.TrySolveAllValues(ctx, bpcsPtr, pcsPtr, mapping, precondition, negate, dropUB, trySolveConstant, maxSolutions, &success);
            Console.WriteLine("Solved!");
            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<ulong>((nint)vecPtr,
                (nint ptr) => *((ulong*)ptr));

            return (managedVec.Items.ToHashSet().AsReadOnly(), success);
        }
    }

    public static class NativeSlicingApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void SliceValue(LLVMOpaqueValue* function, LLVMOpaqueValue* value);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void Solve(LLVMOpaqueValue* function, LLVMOpaqueValue* remillJumpCall, LLVMOpaqueValue* value, LLVMOpaqueBasicBlock* jmpFromBlock, nint lazyValueInfo, nint trySolveConstant);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<ulong>* TrySolveAllValues(SouperOpaqueInstContext* ctx, OpaqueManagedVector<SouperOpaqueBlockPCMapping>* bpcs, OpaqueManagedVector<SouperOpaqueInstMapping>* pcs,
            SouperOpaqueInstMapping* mapping, SouperOpaqueInst* precondition, bool negate, bool dropUB, nint trySolveConstant, int maxSolutions, bool* outSuccess);
    }
}
