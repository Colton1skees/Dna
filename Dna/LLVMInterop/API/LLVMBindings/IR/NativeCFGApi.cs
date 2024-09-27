using Dna.LLVMInterop.API.LLVMBindings.IR;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings
{
    public static class NativeCFGApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern uint BasicBlock_GetPredSize(LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueBasicBlock>* BasicBlock_GetPredecessors(LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern uint BasicBlock_GetSuccSize(LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueBasicBlock>* BasicBlock_GetSuccessors(LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueValue>* Value_GetUsers(LLVMOpaqueValue* block);
    }
}
