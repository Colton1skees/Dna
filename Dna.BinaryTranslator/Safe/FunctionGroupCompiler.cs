using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;
using Dna.Binary.Windows;
using Dna.BinaryTranslator.Lifting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Safe
{
    public class FunctionGroupCompiler
    {
        private readonly IDna dna;

        private readonly PEFile srcPe;

        private readonly PEFile dstPe;

        private readonly IReadOnlyList<SafelyTranslatedFunction> translatedFunctions;

        public static void Compile(IDna dna, IReadOnlyList<SafelyTranslatedFunction> translatedFunctions) => new FunctionGroupCompiler(dna, translatedFunctions).Compile();

        public FunctionGroupCompiler(IDna dna, IReadOnlyList<SafelyTranslatedFunction> translatedFunctions)
        {
            this.dna = dna;
            this.srcPe = (dna.Binary as WindowsBinary).PEFile;
            this.dstPe = PEFile.FromBytes(dna.Binary.Bytes);
            this.translatedFunctions = translatedFunctions;
        }

        private void Compile()
        {
            // For now assume only one function.
            var single = translatedFunctions.Single();

            // Calculate the minimum height required to fit all of our readonly data(which currently only includes pointers to vmexits).
            var readonlySectionHeight = CalculateReadonlySectionHeight();

            var readonlySection = SectionManager.AllocateNewSection(dstPe, ".vmptrs", (uint)readonlySectionHeight, SectionFlags.MemoryRead);

            foreach(var func in translatedFunctions)
            {
                FunctionCompiler.Compile(dna, func, readonlySection.Rva + dna.Binary.BaseAddress);
            }

            Console.WriteLine("");
        }

        private uint CalculateReadonlySectionHeight()
        {
            // Compute the number of VCALLs
            var numVcallPtrs = translatedFunctions.SelectMany(x => x.Runtime.CallKeyToStubVmEnterGlobalPtrs).Count() * 8;

            // Compute the number of VRETs. Note that there is only 1 vmreturn stub for each vm function.
            var numVexitPtrs = translatedFunctions.Count * 8;

            // Allocate an 8 byte ptr for the C_SPECIFIC_HANDLER.
            var numCSpecificHandlerPtrs = (1 * 8);

            return (uint)(numVcallPtrs + numVexitPtrs + numCSpecificHandlerPtrs);
        }
    }
}
