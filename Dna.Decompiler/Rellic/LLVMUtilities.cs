using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Decompiler.Rellic
{
    public static class LlvmUtilities
    {
        public unsafe static byte[] SerializeModuleToBC(LLVMModuleRef module)
        {
            unsafe
            {
                var bufferRef = LLVM.WriteBitcodeToMemoryBuffer((LLVMOpaqueModule*)module.Handle);
                var serialized = GetLlvmBytes(bufferRef);
                LLVM.DisposeMemoryBuffer(bufferRef);
                return serialized;
            }
        }

        private unsafe static byte[] GetLlvmBytes(LLVMOpaqueMemoryBuffer* bufferRef)
        {
            var start = LLVM.GetBufferStart(bufferRef);
            var size = LLVM.GetBufferSize(bufferRef);
            byte[] copy = new byte[size];
            Marshal.Copy((nint)start, copy, 0, (int)size);
            return copy;
        }

        public static string SerialzeModuleToString(LLVMModuleRef module)
        {
            return module.PrintToString();
        }
    }
}
