using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.IO;
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

        public static unsafe LLVMMemoryBufferRef CreateMemoryBuffer(string filePath)
        {
            LLVMMemoryBufferRef handle;
            sbyte* msg;
            if (LLVM.CreateMemoryBufferWithContentsOfFile(new MarshaledString(filePath), (LLVMOpaqueMemoryBuffer**)&handle, &msg) == 0)
            {
 
            }

            return handle;
        }

        public static bool TryParseBitcode(this LLVMContextRef context, LLVMMemoryBufferRef memBuf, out LLVMModuleRef outModule, out string outMessage)
        {
            return context.TryParseIR(memBuf, out outModule, out outMessage);
        }
    }
}
