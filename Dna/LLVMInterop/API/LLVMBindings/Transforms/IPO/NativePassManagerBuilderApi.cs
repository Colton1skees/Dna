using Dna.LLVMInterop.API.LLVMBindings.IR;
using ELFSharp.MachO;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Transforms.IPO
{
    public static class NativePassManagerBuilderApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "PassManagerBuilder_Constructor")]
        public unsafe static extern LLVMOpaquePassManagerBuilder* PassManagerBuilderConstructor();

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "PassManagerBuilder_PopulateFunctionPassManager")]
        public unsafe static extern LLVMOpaquePassManagerBuilder* PopulateFunctionPassManager(LLVMOpaquePassManagerBuilder* pmb, LLVMOpaqueFunctionPassManager* fpm);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "PassManagerBuilder_PopulateModulePassManager")]
        public unsafe static extern LLVMOpaquePassManagerBuilder* PopulateModulePassManager(LLVMOpaquePassManagerBuilder* pmb, LLVMOpaquePassManagerBase* passManagerBase);
    }
}
