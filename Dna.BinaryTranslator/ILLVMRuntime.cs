using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator
{
    public interface ILLVMRuntime
    {
        public bool UsesMemoryPointer { get; }

        public bool UsesVirtualDispatcher { get; }

        public LLVMValueRef? MemoryPointer { get; }
    }
}
