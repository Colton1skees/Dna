using Dna.LLVMInterop.API.RegionAnalysis.Wrapper;
using Dna.Structuring.Stackify.Structured;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna
{
    public static class StructuredPrinter
    {
        private static int indent = 0;

        public static StringBuilder sb = new();

        public static void PrintASTInternal(List<WasmBlock> wasmBlocks)
        {
            foreach (var block in wasmBlocks)
                FormatWasmBlock(block);
        }

        private static void FormatWasmBlock(WasmBlock wasmBlock)
        {
            switch(wasmBlock)
            {
                default:
                    Console.WriteLine("");
                    Console.WriteLine(sb.ToString());
                    throw new InvalidOperationException();
            }
        }
    }
}
