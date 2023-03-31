using Dna.LLVMInterop.API.LLVMBindings;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Dna.LLVMInterop.NativePassApi;

namespace Dna.LLVMInterop
{
    public static class PassApi
    {
        public unsafe static FunctionPass CreateControlledNodeSplittingPass()
        {
            return NativePassApi.CreateControlledNodeSplittingPass();
        }

        public unsafe static FunctionPass CreateUnSwitchPass()
        {
            return NativePassApi.CreateUnSwitchPass();
        }

        public unsafe static FunctionPass CreateLoopExitEnumerationPass()
        {
            return NativePassApi.CreateLoopExitEnumerationPass();
        }

        public unsafe static FunctionPass CreateControlFlowStructuringPass(dgStructureFunction structureFunction)
        {
            return NativePassApi.CreateControlFlowStructuringPass(Marshal.GetFunctionPointerForDelegate(structureFunction));
        }
    }
}
