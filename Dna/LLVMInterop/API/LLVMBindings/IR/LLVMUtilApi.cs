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
    public static class LLVMUtilApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueValue>* Function_GetRpoInstructions(LLVMOpaqueValue* func);

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

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueBasicBlock* SplitBasicBlockAt(LLVMOpaqueBasicBlock* block, LLVMOpaqueValue* inst, sbyte* name, bool before);

        [DllImport("Dna.LLVMInterop")]
        public unsafe static extern void MakeArgNoAlias(LLVMOpaqueValue* arg);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void MakeDsoLocal(LLVMOpaqueValue* function, bool dsoLocal);

        [DllImport("Dna.LLVMInterop")]
        public unsafe static extern void AddNoSideEffectAttributes(LLVMOpaqueValue* value);

    }
}
