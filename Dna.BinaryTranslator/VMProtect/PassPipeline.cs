using Dna.LLVMInterop.API.Optimization;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dna.Utilities;
using Dna.Binary;
using System.Runtime.InteropServices;
using Dna.Extensions;
using Dna.LLVMInterop.API.Remill.BC;
using Dna.Passes;

namespace Dna.BinaryTranslator.VMProtect
{
    public class PassPipeline
    {
        public static LLVMValueRef Run(IBinary bin, LLVMValueRef function, bool dbg = false, bool useCloning = true)
        {
            if(dbg)
            {
                function.GlobalParent.PrintToFile("translatedFunction.ll");
                Console.WriteLine("dbg");
            }

            var newMod = function.GlobalParent;

            int i = 0;
            int c = 1;
            int lastCount = int.MaxValue;
            var func = newMod.GetFunctions().FirstOrDefault(func => func.Name == "vmp_maybe_unsolved_jump");
            while (i < 3)
            {
                Console.WriteLine($"Round {c++}!");
                i++;

                var storeToLoad = new CombinedFixedpointOptPass(bin);
                CombinedFixedpointOptPass.runCount = c;
                var pStoreToLoad = Marshal.GetFunctionPointerForDelegate(storeToLoad.PtrToStoreLoadPropagation);

                var instCombine = new AdhocInstCombinePass();
                var pInstCombine = Marshal.GetFunctionPointerForDelegate(instCombine.PtrToStoreLoadPropagation);

                var multiUseCloning = new MultiUseCloningPass();
                var pMultiUseCloning = Marshal.GetFunctionPointerForDelegate(multiUseCloning.PtrToStoreLoadPropagation);

                OptimizationApi.OptimizeModuleVmp(function.GlobalParent, function, false, false, 0, false, 0, false, false, 0, pStoreToLoad, pInstCombine, useCloning ? pMultiUseCloning : 0);

             
                if (func != null && func.GetUsers().Count > 0)
                {
                    var callers = RemillUtils.CallersOf(func).Where(x => x.InstructionParent.Parent == function);
                    var earlyBail = true;
                    foreach (var op in callers
                        .SelectMany(use => use.GetOperands().SkipLast(1)))
                    {
                        if (op.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                        {
                            earlyBail = false;
                            break;
                        }
                    }

                    if (earlyBail)
                    {
                        Console.WriteLine("!!! EARLY BAIL !!!");
                        break;
                    }
                }

                function.VerifyFunction(LLVMVerifierFailureAction.LLVMAbortProcessAction);

                var count = function.GetInstructions().Count();
                if (storeToLoad.Changed || count < lastCount)
                {
                    i = 0;
                }

                lastCount = count;

                //
                //OptimizationApi.OptimizeMbaModule(newMod, false, false, false);

                //if (i > 0)
                if (false)
                {
                    //var mbaPass = new MbaSimplificationPass(function);
                    //mbaPass.Run();

                    var stackVarPass = new OpaqueStackVarEliminationPass();
                    //var pEliminateStackVars = Marshal.GetFunctionPointerForDelegate(stackVarPass.PtrEliminateStackVars);
                    //OptimizationApi.OptimizeModule(newMod, function, false, false, 0, false, 0, false, false, 0, ptrEliminateStackVars: pEliminateStackVars);
                }

                //newMod.PrintToFile("translatedFunction.ll");
            }



            if (dbg)
            {
                Console.WriteLine("dbgend");
            }
            return function;
        }
    }
}