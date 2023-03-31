using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dna.LLVMInterop.NativePassApi;

namespace Dna.LLVMInterop.Passes
{
    public class ControlFlowStructuringPass
    {
        public dgStructureFunction PtrStructureFunction { get; }

        public unsafe ControlFlowStructuringPass()
        {
            PtrStructureFunction = new dgStructureFunction(StructureFunction);
        }

        public unsafe bool StructureFunction(LLVMOpaqueValue* function, nint loopInfo)
        {
            return StructureFunctionInternal(function, loopInfo);
        }

        private bool StructureFunctionInternal(LLVMValueRef function, nint loopInfo)
        {
            Console.WriteLine($"Structuring function: {function.Name}");
            return false;
        }
    }
}
