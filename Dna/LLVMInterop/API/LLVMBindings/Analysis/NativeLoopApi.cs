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
    public static class NativeLoopApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_GetName")]
        public unsafe static extern sbyte* GetName(LLVMOpaqueLoop* loop);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_GetLoopDepth")]
        public unsafe static extern uint GetLoopDepth(LLVMOpaqueLoop* loop);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_GetHeader")]
        public unsafe static extern LLVMOpaqueBasicBlock* GetHeader(LLVMOpaqueLoop* loop);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_GetParentLoop")]
        public unsafe static extern LLVMOpaqueLoop* GetParentLoop(LLVMOpaqueLoop* loop);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_GetOutermostLoop")]
        public unsafe static extern LLVMOpaqueLoop* GetOutermostLoop(LLVMOpaqueLoop* loop);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_ContainsLoop")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool ContainsLoop(LLVMOpaqueLoop* loop, LLVMOpaqueLoop* l);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_ContainsBlock")]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool ContainsBlock(LLVMOpaqueLoop* loop, LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_ContainsInstruction")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool ContainsInstruction(LLVMOpaqueLoop* loop, LLVMOpaqueValue* instruction);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_GetSubLoops")]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueLoop>* GetSubLoops(LLVMOpaqueLoop* loop);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_IsInnermost")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool IsInnermost(LLVMOpaqueLoop* loop);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_IsOutermost")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool IsOutermost(LLVMOpaqueLoop* loop);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_GetBlocks")]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueBasicBlock>* GetBlocks(LLVMOpaqueLoop* loop);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_IsLoopExiting")]
        [return: MarshalAs(UnmanagedType.U1)] 
        public unsafe static extern bool IsLoopExiting(LLVMOpaqueLoop* loop, LLVMOpaqueBasicBlock* block);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_GetNumBackEdges")]
        public unsafe static extern int GetNumBackEdges(LLVMOpaqueLoop* loop);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_GetExitingBlocks")]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueBasicBlock>* GetExitingBlocks(LLVMOpaqueLoop* loop);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Loop_GetExitBlocks")]
        public unsafe static extern OpaqueManagedVector<LLVMOpaqueBasicBlock>* GetExitBlocks(LLVMOpaqueLoop* loop);
    }
}
