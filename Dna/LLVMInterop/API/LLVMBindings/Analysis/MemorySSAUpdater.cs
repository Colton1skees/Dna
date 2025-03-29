using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Analysis
{
    public struct LLVMOpaqueMemorySSAUpdater { };

	public class MemorySSAUpdater : IDisposable
	{
		unsafe LLVMOpaqueMemorySSAUpdater* handle;

		public MemorySSAUpdater(MemorySSA memSsa)
		{
			unsafe
			{
				handle = NativeMemorySSAUpdaterApi.Get(memSsa);
			}
		}

        public unsafe MemoryUseOrDef CreateMemoryAccessBefore(LLVMValueRef inst, MemoryAccess definition, MemoryUseOrDef insertPoint)
        {
            return NativeMemorySSAUpdaterApi.MemorySsaUpdater_CreateMemoryAccessBefore(handle, inst, definition, insertPoint);
        }

        public unsafe void InsertUse(MemoryUseOrDef use, bool renameUses)
        {
            NativeMemorySSAUpdaterApi.MemorySsaUpdater_InsertUse(handle, use, renameUses);
        }

		public unsafe void RemoveMemoryAccess(LLVMValueRef instr)
		{
            NativeMemorySSAUpdaterApi.RemoveMemAccess(handle, instr.Handle);
        }

        public unsafe void Dispose()
        {
            NativeMemorySSAUpdaterApi.Delete(handle);
        }
    }

    public static class NativeMemorySSAUpdaterApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSAUpdater_Get")]
        public unsafe static extern LLVMOpaqueMemorySSAUpdater* Get(LLVMOpaqueMemorySSA* memSsa);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern LLVMOpaqueMemoryUseOrDef* MemorySsaUpdater_CreateMemoryAccessBefore(LLVMOpaqueMemorySSAUpdater* memSsa, LLVMOpaqueValue* inst, LLVMOpaqueMemoryAccess* definition, LLVMOpaqueMemoryUseOrDef* insertPoint);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void MemorySsaUpdater_InsertUse(LLVMOpaqueMemorySSAUpdater* memSsa, LLVMOpaqueMemoryUseOrDef* use, bool renameUses);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSAUpdater_RemoveMemAccess")]
        public unsafe static extern void RemoveMemAccess(LLVMOpaqueMemorySSAUpdater* memSsa, nint inst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MemorySSAUpdater_Delete")]
        public unsafe static extern void Delete(LLVMOpaqueMemorySSAUpdater* memSsa);
    }
}