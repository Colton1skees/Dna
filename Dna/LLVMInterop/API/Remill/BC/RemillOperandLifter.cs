using Dna.LLVMInterop.API.Remill.Manual;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Remill.BC
{
    public class RemillOperandLifter
    {
        public readonly nint Handle;

        public unsafe LLVMTypeRef MemoryType => NativeRemillLifterApi.OperandLifter_GetMemoryType(this);

        public RemillOperandLifter(nint handle)
        {
            Handle = handle;
        }

        public unsafe LLVMValueRef LoadRegAddress(LLVMBasicBlockRef block, LLVMValueRef statePtr, string regName) => NativeRemillLifterApi.OperandLifter_LoadRegAddress(this, block, statePtr, new MarshaledString(regName));

        public unsafe LLVMValueRef OperandLifter_LoadRegValue(LLVMBasicBlockRef block, LLVMValueRef statePtr, string regName) => NativeRemillLifterApi.OperandLifter_LoadRegValue(this, block, statePtr, new MarshaledString(regName));

        public unsafe void ClearCache() => NativeRemillLifterApi.OperandLifter_ClearCache(this);

        public unsafe static implicit operator RemillOpaqueOperandLifter*(RemillOperandLifter reg) => (RemillOpaqueOperandLifter*)reg.Handle;

        public unsafe static implicit operator RemillOperandLifter(RemillOpaqueOperandLifter* reg) => new RemillOperandLifter((nint)reg);
    }
}
