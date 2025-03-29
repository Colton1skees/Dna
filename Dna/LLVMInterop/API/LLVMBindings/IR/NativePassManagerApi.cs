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
    public static class NativePassManagerApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "PassManager_Constructor")]
        public unsafe static extern LLVMOpaquePassManager* PassManagerConstructor();

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "PassManagerBase_Add")]
        public unsafe static extern void AddPass(LLVMOpaquePassManagerBase* passManager, LLVMOpaquePass* pass);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "PassManager_Run")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool RunOnModule(LLVMOpaquePassManager* passManager, LLVMOpaqueModule* module);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FunctionPassManager_Constructor")]
        public unsafe static extern LLVMOpaqueFunctionPassManager* FunctionPassManagerConstructor();

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FunctionPassManager_Run")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool RunOnFunction(LLVMOpaqueFunctionPassManager* passManager, LLVMOpaqueValue* func);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FunctionPassManager_DoInitialization")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool FunctionPassManagerDoInitialization(LLVMOpaqueFunctionPassManager* passManager);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FunctionPassManager_DoFinalization")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool FunctionPassManagerDoFinalization(LLVMOpaqueFunctionPassManager* passManager);
    }
}
