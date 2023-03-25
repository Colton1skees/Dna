using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.RegionAnalysis.Wrapper
{
    public class SwitchRegion : ComplexRegion
    {
        public SwitchRegion(nint handle) : base(handle)
        {
        }
    }
}
