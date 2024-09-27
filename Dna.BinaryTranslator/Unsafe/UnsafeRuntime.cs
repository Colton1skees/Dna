using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Unsafe
{
    public class UnsafeRuntime : ILLVMRuntime
    {
        public bool UsesVirtualDispatcher => true;

        public bool UsesMemoryPointer => true;

        public LLVMValueRef? MemoryPointer { get; }

        public UnsafeRuntime(LLVMValueRef memoryPtr)
        {
            MemoryPointer = memoryPtr;
        }
    }
}
