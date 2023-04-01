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
    public static class NativeMemoryPhiApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemoryPhi_GetBlocks")]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueBasicBlock>* GetBlocks(LLVMOpaqueMemoryPhi* memPhi);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemoryPhi_GetIncomingValues")]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueUse>* GetIncomingValues(LLVMOpaqueMemoryPhi* memPhi);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemoryPhi_GetNumIncomingValues")]
        public unsafe static extern uint GetNumIncomingValues(LLVMOpaqueMemoryPhi* memPhi);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemoryPhi_GetIncomingValue")]
        public unsafe static extern LLVMOpaqueMemoryAccess* GetIncomingValue(LLVMOpaqueMemoryPhi* memPhi, uint index);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemoryPhi_GetIncomingValueForBlock")]
        public unsafe static extern LLVMOpaqueMemoryAccess* GetIncomingValueForBlock(LLVMOpaqueMemoryPhi* memPhi, LLVMOpaqueBasicBlock* block);
    }
}
