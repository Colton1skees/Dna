using Dna.LLVMInterop.API.Remill.Manual;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Remill.BC
{
    public class RemillIntrinsicTable
    {
        public readonly nint Handle;

        public unsafe LLVMValueRef Error => NativeRemillIntrinsicTableApi.IntrinsicTable_GetError(this);

        public unsafe LLVMValueRef FunctionCall => NativeRemillIntrinsicTableApi.IntrinsicTable_GetFunctionCall(this);

        public unsafe LLVMValueRef FunctionReturn => NativeRemillIntrinsicTableApi.IntrinsicTable_GetFunctionReturn(this);

        public unsafe LLVMValueRef Jump => NativeRemillIntrinsicTableApi.IntrinsicTable_GetJump(this);

        public unsafe LLVMValueRef MissingBlock => NativeRemillIntrinsicTableApi.IntrinsicTable_GetMissingBlock(this);

        public unsafe LLVMValueRef SyncHyperCall => NativeRemillIntrinsicTableApi.IntrinsicTable_GetSyncHyperCall(this);

        public unsafe LLVMValueRef ReadMemory8 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetReadMemory8(this);

        public unsafe LLVMValueRef ReadMemory16 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetReadMemory16(this);

        public unsafe LLVMValueRef ReadMemory32 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetReadMemory32(this);

        public unsafe LLVMValueRef ReadMemory64 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetReadMemory64(this);

        public unsafe LLVMValueRef ReadMemoryF32 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetReadMemoryF32(this);

        public unsafe LLVMValueRef ReadMemoryF64 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetReadMemoryF64(this);

        public unsafe LLVMValueRef ReadMemoryF80 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetReadMemoryF80(this);

        public unsafe LLVMValueRef ReadMemoryF128 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetReadMemoryF128(this);

        public unsafe LLVMValueRef WriteMemory8 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetWriteMemory8(this);

        public unsafe LLVMValueRef WriteMemory16 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetWriteMemory16(this);

        public unsafe LLVMValueRef WriteMemory32 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetWriteMemory32(this);

        public unsafe LLVMValueRef WriteMemory64 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetWriteMemory64(this);

        public unsafe LLVMValueRef WriteMemoryF32 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetWriteMemoryF32(this);

        public unsafe LLVMValueRef WriteMemoryF64 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetWriteMemoryF64(this);

        public unsafe LLVMValueRef WriteMemoryF80 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetWriteMemoryF80(this);

        public unsafe LLVMValueRef WriteMemoryF128 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetWriteMemoryF128(this);

        public unsafe LLVMValueRef BarrierLoadLoad => NativeRemillIntrinsicTableApi.IntrinsicTable_GetBarrierLoadLoad(this);

        public unsafe LLVMValueRef BarrierLoadStore => NativeRemillIntrinsicTableApi.IntrinsicTable_GetBarrierLoadStore(this);

        public unsafe LLVMValueRef BarrierStoreLoad => NativeRemillIntrinsicTableApi.IntrinsicTable_GetBarrierStoreLoad(this);

        public unsafe LLVMValueRef BarrierStoreStore => NativeRemillIntrinsicTableApi.IntrinsicTable_GetBarrierStoreStore(this);

        public unsafe LLVMValueRef AtomicBegin => NativeRemillIntrinsicTableApi.IntrinsicTable_GetAtomicBegin(this);

        public unsafe LLVMValueRef AtomicEnd => NativeRemillIntrinsicTableApi.IntrinsicTable_GetAtomicEnd(this);

        public unsafe LLVMValueRef DelaySlotBegi => NativeRemillIntrinsicTableApi.IntrinsicTable_GetDelaySlotBegin(this);

        public unsafe LLVMValueRef DelaySlotEnd => NativeRemillIntrinsicTableApi.IntrinsicTable_GetDelaySlotEnd(this);

        public unsafe LLVMValueRef Undefined8 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetUndefined8(this);

        public unsafe LLVMValueRef GetUndefined16 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetUndefined16(this);

        public unsafe LLVMValueRef GetUndefined32 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetUndefined32(this);

        public unsafe LLVMValueRef GetUndefined64 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetUndefined64(this);

        public unsafe LLVMValueRef GetUndefinedF32 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetUndefinedF32(this);

        public unsafe LLVMValueRef GetUndefinedF64 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetUndefinedF64(this);

        public unsafe LLVMValueRef UndefinedF80 => NativeRemillIntrinsicTableApi.IntrinsicTable_GetUndefinedF80(this);

        public unsafe LLVMValueRef FlagComputationZero => NativeRemillIntrinsicTableApi.IntrinsicTable_GetFlagComputationZero(this);

        public unsafe LLVMValueRef FlagComputationSign => NativeRemillIntrinsicTableApi.IntrinsicTable_GetFlagComputationSign(this);

        public unsafe LLVMValueRef FlagComputationOverflow => NativeRemillIntrinsicTableApi.IntrinsicTable_GetFlagComputationOverflow(this);

        public unsafe LLVMValueRef FlagComputationCarry => NativeRemillIntrinsicTableApi.IntrinsicTable_GetFlagComputationCarry(this);

        public unsafe LLVMValueRef CompareSle => NativeRemillIntrinsicTableApi.IntrinsicTable_GetCompareSle(this);

        public unsafe LLVMValueRef CompareSgt => NativeRemillIntrinsicTableApi.IntrinsicTable_GetCompareSgt(this);

        public unsafe LLVMValueRef CompareEq => NativeRemillIntrinsicTableApi.IntrinsicTable_GetCompareEq(this);

        public unsafe LLVMValueRef CompareNeq => NativeRemillIntrinsicTableApi.IntrinsicTable_GetCompareNeq(this);

        public unsafe LLVMTypeRef LiftedFunctionType => NativeRemillIntrinsicTableApi.IntrinsicTable_GetLiftedFunctionType(this);

        public unsafe LLVMTypeRef StatePtrType => NativeRemillIntrinsicTableApi.IntrinsicTable_GetStatePtrType(this);

        public unsafe LLVMTypeRef PcType => NativeRemillIntrinsicTableApi.IntrinsicTable_GetPcType(this);

        public unsafe LLVMTypeRef PointerType => NativeRemillIntrinsicTableApi.IntrinsicTable_GetPointerType(this);


        public RemillIntrinsicTable(nint handle)
        {
            Handle = handle;
        }

        public unsafe static implicit operator RemillOpaqueIntrinsicTable*(RemillIntrinsicTable reg) => (RemillOpaqueIntrinsicTable*)reg.Handle;

        public unsafe static implicit operator RemillIntrinsicTable(RemillOpaqueIntrinsicTable* reg) => new RemillIntrinsicTable((nint)reg);
    }
}
