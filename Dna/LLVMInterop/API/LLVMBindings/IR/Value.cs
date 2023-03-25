using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.IR
{
    public class Value
    {
        private readonly LLVMValueRef handle;



        public Value(LLVMValueRef handle)
        {
            this.handle = handle;
        }
    }
}
