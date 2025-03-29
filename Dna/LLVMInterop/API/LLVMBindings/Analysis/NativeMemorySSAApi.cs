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
    public static class NativeMemorySSAApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSA_GetWalker")]
        public unsafe static extern LLVMOpaqueMemorySSAWalker* GetWalker(LLVMOpaqueMemorySSA* memSsa);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSA_GetSkipSelfWalker")]
        public unsafe static extern LLVMOpaqueMemorySSAWalker* GetSkipSelfWalker(LLVMOpaqueMemorySSA* memSsa);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSA_GetMemoryAccessFromInstruction")]
        public unsafe static extern LLVMOpaqueMemoryUseOrDef* GetMemoryAccess(LLVMOpaqueMemorySSA* memSsa, LLVMOpaqueValue* inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSA_GetMemoryAccessFromBlock")]
        public unsafe static extern LLVMOpaqueMemoryPhi* GetMemoryAccessFromBlock(LLVMOpaqueMemorySSA* memSsa, LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSA_IsLiveOnEntryDef")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool IsLiveOnEntryDef(LLVMOpaqueMemorySSA* memSsa, LLVMOpaqueMemoryAccess* memAccess);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSA_GetLiveOnEntryDef")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool GetLiveOnEntryDef(LLVMOpaqueMemorySSA* memSsa);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSA_GetBlockAccesses")]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueMemoryAccess>* GetBlockAccesses(LLVMOpaqueMemorySSA* memSsa, LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSA_GetBlockDefs")]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueMemoryAccess>* GetBlockDefs(LLVMOpaqueMemorySSA* memSsa, LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSA_LocallyDominates")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool LocallyDominates(LLVMOpaqueMemorySSA* memSsa, LLVMOpaqueMemoryAccess* a, LLVMOpaqueMemoryAccess* b);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSA_Dominates")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Dominates(LLVMOpaqueMemorySSA* memSsa, LLVMOpaqueMemoryAccess* a, LLVMOpaqueMemoryAccess* b);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSA_DominatesUse")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool Dominates(LLVMOpaqueMemorySSA* memSsa, LLVMOpaqueMemoryAccess* a, LLVMOpaqueUse* b);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSA_MayAlias")]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool MayAlias(LLVMOpaqueMemorySSA* memSsa, LLVMOpaqueMemoryUseOrDef* use, LLVMOpaqueMemoryUseOrDef* def);
    }
}
