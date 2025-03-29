using Dna.Binary;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.Passes.Matchers;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dna.LLVMInterop.NativePassApi;

namespace Dna.Passes
{
    public class OpaqueStackVarEliminationPass
    {
        public dgCombinedFixedpointPass PtrEliminateStackVars { get; }

        private LLVMValueRef function;

        private LoopInfo loopInfo;

        private MemorySSA mssa;

        private readonly LLVMValueRef rsp;

        public unsafe OpaqueStackVarEliminationPass()
        {
            PtrEliminateStackVars = new dgCombinedFixedpointPass(StackVarElimination);
        }

        private unsafe bool StackVarElimination(LLVMOpaqueValue* function, nint loopInfo, nint mssa)
        {
            this.function = function;
            this.loopInfo = new LoopInfo(loopInfo);
            this.mssa = new MemorySSA(mssa);
            return Run();
        }

        private bool Run()
        {
            var loads = function.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad);
            foreach(var load in loads)
            {
                ProcessLoad(load);
            }

            return false;
        }

        private void ProcessLoad(LLVMValueRef loadInst)
        {
            // Get the GEP.
            var gep = loadInst.GetOperand(0);
            if (gep.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr)
                return;

            // Skip if this is not of the pattern: %foo = add i64 %rsp, wildcard
            var gepIndex = gep.GetOperand(1);
            if (!StackAccessMatcher.IsAddToRSP(gepIndex))
                return;

            // Get the offset being added to RSP. If it is not a constant then skip it.
            var other = gepIndex.GetOperand(1);
            if (other.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                return;

            // Get the constant offset. If it is not accessing the local variable section(rsp - 0xwhatever) then skip it.
            // Note that we ignore [rsp - 7] through [rsp - 0] because some of those loads could potentially partially read into arguments.
            var offset = other.ConstIntSExt;
            if (offset >= -8)
                return;

            var isClobbered = IsLoadPossiblyClobbered(loadInst);
            Console.WriteLine($@"Is {loadInst} clobbered: {isClobbered}");

           // Debugger.Break();
        }

        // Returns true if any store before the load instruction could possibly 
        // alias with the load destination.
        private bool IsLoadPossiblyClobbered(LLVMValueRef loadInst)
        {
            var initial = mssa.GetMemoryAccess(loadInst);
            if (!initial.IsOptimized)
                return true;

            MemoryAccess current = initial;

            // If the load has a efdinition outside of this block(aka a memory phi),
            // we terminate and stop processing the load completely.
            if (current is MemoryPhi memoryPhi)
                return true;

            // If we've reached the entry definition, then no stores could possibly clobber this load.
            var useOrDef = (MemoryUseOrDef)current;
            if (mssa.IsLiveOnEntryDef(useOrDef))
                return false;

            var memoryInst = useOrDef.MemoryInst;
            if (memoryInst == null)
            {
                Debugger.Break();
            }

            else
            {
                current = mssa.Walker.GetClobberingMemoryAccess(memoryInst);
                Console.WriteLine($"{current} with memoryinst: {((MemoryUseOrDef)current).MemoryInst}");
                //Debugger.Break();
            }

            return false;
        }
    }
}
