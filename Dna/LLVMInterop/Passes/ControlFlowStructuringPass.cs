using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.DataStructures;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Dna.LLVMInterop.NativePassApi;

namespace Dna.LLVMInterop.Passes
{
    public class ControlFlowStructuringPass
    {
        private bool isStructured = false;

        private ImmutableDomTree<LLVMValueRef> domTree;

        private LoopInfo loopInfo;

        private ControlFlowGraph<LLVMValueRef> cfg;

        private HashSet<LLVMBasicBlockRef> loopHeaders;

        private HashSet<LLVMBasicBlockRef> loopExits;

        private HashSet<BasicBlock<LLVMValueRef>> visited = new();

        private HashSet<BlockEdge<LLVMValueRef>> processedEdges = new();

        private StringBuilder sb = new();

        private int indent = 0;

        private OrderedSet<BasicBlock<LLVMValueRef>> blockQueue = new();

        public dgStructureFunction PtrStructureFunction { get; }

        public unsafe ControlFlowStructuringPass()
        {
            PtrStructureFunction = new dgStructureFunction(StructureFunction);
        }

        public unsafe bool StructureFunction(LLVMOpaqueValue* function, nint loopInfo)
        {
            if (isStructured)
                throw new InvalidOperationException();
            isStructured = true;
            return StructureFunctionInternal(function, loopInfo);
        }

        private bool StructureFunctionInternal(LLVMValueRef function, nint pLoopInfo)
        {
            Console.WriteLine($"Structuring function: {function.Name}");
            function.GlobalParent.PrintToFile("parent_before_structuring.ll");

            cfg = LLVMToCFG.GetCFG(function);
            domTree = new ImmutableDomTree<LLVMValueRef>(cfg);
            loopInfo = new LoopInfo(pLoopInfo);

            loopHeaders = function.BasicBlocks.Where(x => loopInfo.IsLoopHeader(x)).ToHashSet();
            loopExits = loopInfo.LoopsInReverseSiblingPreorder.SelectMany(x => x.ExitBlocks).ToHashSet();
            return false;
        }

    }
}
