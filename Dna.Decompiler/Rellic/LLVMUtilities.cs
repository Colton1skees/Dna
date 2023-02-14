using LLVMSharp;
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
        public static byte[] SerializeModuleToBC(LLVMModuleRef module)
        {
            var bufferRef = LLVM.WriteBitcodeToMemoryBuffer(module);
            var serialized = GetLlvmBytes(bufferRef);
            LLVM.DisposeMemoryBuffer(bufferRef);
            return serialized;
        }

        private static byte[] GetLlvmBytes(LLVMMemoryBufferRef bufferRef)
        {
            IntPtr start = LLVMSharp.LLVM.GetBufferStart(bufferRef);
            var size = LLVMSharp.LLVM.GetBufferSize(bufferRef);
            byte[] copy = new byte[size];
            Marshal.Copy(start, copy, 0, size);
            return copy;
        }
    }
}
