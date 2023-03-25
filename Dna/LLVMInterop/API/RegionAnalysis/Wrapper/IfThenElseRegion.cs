using Dna.LLVMInterop.API.RegionAnalysis.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.RegionAnalysis.Wrapper
{
    public class IfThenElseRegion : IfThenRegion
    {
        public Region Else => NativeIfThenElseRegionApi.IfThenElseRegionGetElse(handle);

        public IfThenElseRegion(nint handle) : base(handle)
        {

        }
    }
}
