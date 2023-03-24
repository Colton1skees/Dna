using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Wrapper
{
    public class BlockRegion : Region
    {
        public BlockRegion(nint handle) : base(handle)
        {
        }
    }
}
