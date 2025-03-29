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
    public static unsafe class NativeLoopInfoApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LoopInfo_Constructor")]
        public unsafe static extern LLVMOpaqueLoopInfo* Constructor();

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LoopInfo_GetLoopsInPreorder")]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueLoop>* GetLoopsInPreOrder(LLVMOpaqueLoopInfo* loopInfo);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LoopInfo_GetLoopsInReverseSiblingPreorder")]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueLoop>* GetLoopsInReverseSiblingPreorder(LLVMOpaqueLoopInfo* loopInfo);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LoopInfo_GetLoopFor")]
        public unsafe static extern LLVMOpaqueLoop* GetLoopFor(LLVMOpaqueLoopInfo* loopInfo, LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LoopInfo_GetLoopDepth")]
        public unsafe static extern uint GetLoopDepth(LLVMOpaqueLoopInfo* loopInfo, LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LoopInfo_IsLoopheader")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool IsLoopHeader(LLVMOpaqueLoopInfo* loopInfo, LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LoopInfo_GetTopLevelLoops")]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueLoop>* GetTopLevelLoops(LLVMOpaqueLoopInfo* loopInfo);
    }
}
