using ELFSharp.MachO;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings
{
    // Different types of passes.
    public enum PassKind : byte
    {
        Region,
        Loop,
        Function,
        CallGraphSCC,
        Module,
        PassManager
    };

    public static class NativePassApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Pass_GetPassKind")]
        public unsafe static extern PassKind GetPassKind(LLVMOpaquePass* pass);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Pass_GetPassName")]
        public unsafe static extern sbyte* GetPassName(LLVMOpaquePass* pass);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ModulePass_RunOnModule")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool RunOnModule(LLVMOpaqueModulePass* pass, LLVMOpaqueModule* module);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FunctionPass_RunOnFunction")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool RunOnFunction(LLVMOpaqueFunctionPass* pass, LLVMOpaqueValue* function);
    }
}
