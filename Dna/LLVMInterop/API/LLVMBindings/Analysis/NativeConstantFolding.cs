using ELFSharp.MachO;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Analysis
{
    public static class ConstantFoldingAPI
    {
        public static unsafe LLVMValueRef TryConstantFold(LLVMValueRef inst)
        {
            return NativeConstantFoldingAPI.TryConstantFold(inst);
        }

        public static unsafe LLVMValueRef TrySimplify(LLVMValueRef inst)
        {
            return NativeConstantFoldingAPI.TrySimplify(inst);
        }

        public static unsafe bool IsInstructionTriviallyDead(LLVMValueRef inst)
        {
            return NativeConstantFoldingAPI.IsInstructionTriviallyDead(inst);
        }

        public static unsafe void DropPoisonGeneratingFlags(LLVMValueRef inst)
        {
            NativeConstantFoldingAPI.DropPoisonGeneratingFlags(inst);
        }
    }

    public static class NativeConstantFoldingAPI
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "TryConstantFold")]
        public unsafe static extern LLVMOpaqueValue* TryConstantFold(LLVMOpaqueValue* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "TrySimplify")]
        public unsafe static extern LLVMOpaqueValue* TrySimplify(LLVMOpaqueValue* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "IsInstructionTriviallyDead")]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool IsInstructionTriviallyDead(LLVMOpaqueValue* inst);


        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "DropPoisonGeneratingFlags")]
        public unsafe static extern void DropPoisonGeneratingFlags(LLVMOpaqueValue* inst);
    }
}
