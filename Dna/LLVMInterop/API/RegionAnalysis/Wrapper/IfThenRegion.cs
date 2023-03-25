using Dna.LLVMInterop.API.RegionAnalysis.Native;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.RegionAnalysis.Wrapper
{
    public class IfThenRegion : ComplexRegion
    {
        public Region Then => NativeIfThenRegionApi.IfThenRegionGetThen(handle);

        public bool IsNegated => NativeIfThenRegionApi.IfThenRegionGetIsNegated(handle);

        public LLVMValueRef TerminatorInstruction => new LLVMValueRef(NativeIfThenRegionApi.IfThenRegionGetLLVMTerminatorInstruction(handle));

        public IfThenRegion(nint handle) : base(handle)
        {

        }
    }
}
