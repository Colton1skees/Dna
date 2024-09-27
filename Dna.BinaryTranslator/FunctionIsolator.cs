using Dna.BinaryTranslator.Lifting;
using Dna.Extensions;
using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.BC;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator
{
    public static class FunctionIsolator
    {
        public static unsafe LLVMValueRef IsolateFunctionIntoNewModule(RemillArch arch, LLVMValueRef function)
        {
            File.WriteAllText("translatedFunction.ll", function.PrintToString());
            // Console.WriteLine("");
            //Console.WriteLine(function);
            // Optimize the lifted function(mainly to inline intrinsics).
            // Note: Maybe we shouldn't optimize at all here?
            RemillOptimizer.OptimizeFunction(arch, function);

            // Create a new remill module.
            var guid = Guid.NewGuid().ToString();
            var outModule = function.GlobalParent.Context.CreateModuleWithName("outmodule" + guid);
            outModule.Target = "x86_64-pc-windows-msvc";
            arch.PrepareModuleDataLayout(outModule);

            // Move the lifted function into the new module.
            RemillUtils.MoveFunctionIntoModule(function, outModule);
            outModule.Target = "x86_64-pc-windows-msvc";
            return outModule.GetNamedFunction(function.Name);
        }

        // Implementation of `IsolateFunctionIntoNewModule` that also copies over SEH related functions(both SEH filters and C personality functions).
        public static unsafe (LLVMValueRef function, IReadOnlyList<LiftedFilterFunction> filterFunctions) IsolateFunctionIntoNewModuleWithSehSupport(RemillArch arch, LLVMValueRef function, IReadOnlyList<LiftedFilterFunction> filterFunctions)
        {
            bool hasPersonality = function.GlobalParent.GetNamedFunction("__C_specific_handler").Handle != 0;
            var isolatedFunction = IsolateFunctionIntoNewModule(arch, function);
            var outModule = isolatedFunction.GlobalParent;

            // Copy over all of the filter functions.
            var newFilters = new List<LiftedFilterFunction>();
            foreach (var filter in filterFunctions)
            {
                RemillUtils.MoveFunctionIntoModule(filter.LlvmFunction, isolatedFunction.GlobalParent);
                var f = outModule.GetNamedFunction(filter.LlvmFunction.Name);
                var rsp = outModule.GetNamedGlobal(filter.RspGlobal.Name);
                var imagebase = outModule.GetNamedGlobal(filter.ImagebaseGlobal.Name);
                newFilters.Add(new LiftedFilterFunction(filter.Address, f, rsp, imagebase));
            }

            // If a personality function was previously present, copy it over to the target module.
            // For some reason it is getting deleted by `IsolateFunctionIntoNewModule`.
            if (hasPersonality)
            {
                var builder = new SehIntrinsicBuilder(isolatedFunction.GlobalParent, LLVMBuilderRef.Create(isolatedFunction.GetFunctionCtx()));
                builder.CreateMsvcPersonalityFunction();
            }

            isolatedFunction.GlobalParent.PrintToFile("translatedFunction.ll");
            return (isolatedFunction, newFilters);
        }
    }
}
