using Dna.BinaryTranslator.Unsafe;
using Dna.LLVMInterop.API.Remill.Arch;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.VMProtect
{
    public static class VmpDevirtualizer
    {
        public static LLVMValueRef Run(IDna dna, ulong addr)
        {
            // Lift the VMP function to LLVM IR
            var ctx = LLVMContextRef.Global;
            var arch = new RemillArch(ctx, RemillOsId.kOSLinux, RemillArchId.kArchAMD64_AVX512);
            var translator = new IterativeVmpTranslator(dna, arch, ctx, addr);
            var function = translator.Run();

            // Optimize the function again.
            function = PassPipeline.Run(dna.Binary, function);

            // Try to eliminate the VMP context structure.
            VmpContextRemovalPass.Run(function);

            // Return the deobfuscated function.
            return function;
        }
    }
}
