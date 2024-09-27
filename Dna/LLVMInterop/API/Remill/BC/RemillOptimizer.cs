using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.BC;
using Dna.LLVMInterop.API.Remill.Manual;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Remill.BC
{ 
    public class RemillOptimizer
    {
        public static void OptimizeFunction(RemillArch arch, LLVMValueRef func)
        {
            OptimizeModule(arch, func.GlobalParent, new List<LLVMValueRef>() { func }.AsReadOnly());
        }

        public static unsafe void OptimizeModule(RemillArch arch, LLVMModuleRef module, IReadOnlyList<LLVMValueRef> functions)
        {
            var funcArray = functions.ToArray();
            fixed (LLVMValueRef* pArr = funcArray)
            {
                var ptr = (LLVMOpaqueValue**)pArr;
                NativeRemillOptimizerApi.OptimizeModule(arch, module, ptr, funcArray.Length);
            };
        }

        public static unsafe void OptimizeBareModule(LLVMModuleRef module)
        {
            NativeRemillOptimizerApi.OptimizeBareModule(module);
        }
    }
}
