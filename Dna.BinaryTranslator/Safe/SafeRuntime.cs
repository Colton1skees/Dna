using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Safe
{
    internal class SafeRuntime : ILLVMRuntime
    {
        public bool UsesMemoryPointer => false;

        public bool UsesVirtualDispatcher => true;

        public LLVMValueRef? MemoryPointer => null;
    }
}
