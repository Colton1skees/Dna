using Dna.LLVMInterop.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Wrapper
{
    public class IfThenElseRegion : IfThenRegion
    {
        public Region Else => NativeIfThenElseRegionApi.IfThenElseRegionGetElse(handle);

        public IfThenElseRegion(nint handle) : base(handle)
        {

        }
    }
}
