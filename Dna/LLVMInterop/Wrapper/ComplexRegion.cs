using Dna.LLVMInterop.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Wrapper
{
    public abstract unsafe class ComplexRegion : Region
    {
        public Region Head => NativeComplexRegionApi.ComplexRegionGetHead(handle);

        public ComplexRegion(nint handle) : base(handle)
        {

        }

        public static implicit operator ComplexRegion(nint value) => (ComplexRegion)(value);
    }
}
