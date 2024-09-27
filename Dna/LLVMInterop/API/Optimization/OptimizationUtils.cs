using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Optimization
{
    public static class OptimizationUtils
    {
        public static unsafe IReadOnlyList<LLVMBasicBlockRef> FindReachableNodes(LLVMBasicBlockRef source, LLVMBasicBlockRef target)
        {
            var vecPtr = NativeOptimizationUtilsApi.FindReachableNodes(source, target);

            // Convert the ptr to a typed managed vector.
            var managedVec = new ManagedVector<LLVMBasicBlockRef>((nint)vecPtr,
                (nint ptr) => new LLVMBasicBlockRef(ptr));

            // Return the read only list.
            return managedVec.Items;
        }
    }
}
