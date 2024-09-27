using AsmResolver.PE.Exceptions.X64;
using Dna.Binary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.SEH
{
    public class RuntimeFunction
    {
        private readonly X64RuntimeFunction runtimeFunction;

        public RuntimeFunction(X64RuntimeFunction runtimeFunction)
        {
            this.runtimeFunction = runtimeFunction;
        }
    }
}
