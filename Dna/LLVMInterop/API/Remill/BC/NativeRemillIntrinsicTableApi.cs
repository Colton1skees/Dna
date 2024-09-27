using Dna.LLVMInterop.API.Remill.Manual;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Remill.BC
{
    public static class NativeRemillIntrinsicTableApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetError(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetFunctionCall(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetFunctionReturn(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetJump(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetMissingBlock(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetSyncHyperCall(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetReadMemory8(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetReadMemory16(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetReadMemory32(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetReadMemory64(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetReadMemoryF32(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetReadMemoryF64(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetReadMemoryF80(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetReadMemoryF128(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetWriteMemory8(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetWriteMemory16(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetWriteMemory32(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetWriteMemory64(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetWriteMemoryF32(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetWriteMemoryF64(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetWriteMemoryF80(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetWriteMemoryF128(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetBarrierLoadLoad(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetBarrierLoadStore(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetBarrierStoreLoad(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetBarrierStoreStore(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetAtomicBegin(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetAtomicEnd(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetDelaySlotBegin(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetDelaySlotEnd(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetUndefined8(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetUndefined16(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetUndefined32(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetUndefined64(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetUndefinedF32(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetUndefinedF64(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetUndefinedF80(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetFlagComputationZero(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetFlagComputationSign(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetFlagComputationOverflow(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetFlagComputationCarry(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetCompareSle(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetCompareSgt(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetCompareEq(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueValue* IntrinsicTable_GetCompareNeq(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueType* IntrinsicTable_GetLiftedFunctionType(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueType* IntrinsicTable_GetStatePtrType(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueType* IntrinsicTable_GetPcType(RemillOpaqueIntrinsicTable* it);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueType* IntrinsicTable_GetPointerType(RemillOpaqueIntrinsicTable* it);
    }
}
