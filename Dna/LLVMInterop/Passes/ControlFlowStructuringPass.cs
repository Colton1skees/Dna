using Dna.Binary;
using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.LLVMInterop.Passes.Matchers;
using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Dna.LLVMInterop.NativePassApi;

namespace Dna.LLVMInterop.Passes
{
    public class ControlFlowStructuringPass
    {
        public static IBinary binary;


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

        public unsafe bool StructureFunction(LLVMOpaqueValue* function, nint loopInfo, nint mssa)
        {
            if (isStructured)
                throw new InvalidOperationException();
            isStructured = false;
            return StructureFunctionInternal(function, loopInfo, mssa);
        }

        private bool StructureFunctionInternal(LLVMValueRef function, nint pLoopInfo, nint m)
        {
            Console.WriteLine($"Structuring function: {function.Name}");
            function.GlobalParent.PrintToFile("parent_before_structuring.ll");

            cfg = LLVMToCFG.GetCFG(function);
            domTree = new ImmutableDomTree<LLVMValueRef>(cfg);
            loopInfo = new LoopInfo(pLoopInfo);

            var mssa = new MemorySSA(m);

            var replacementMapping = new Dictionary<LLVMValueRef, LLVMValueRef>();

            foreach(var load in function.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad))
            {
                // Get the memory access.
                var instAccess = mssa.GetMemoryAccess(load);

                if(load.ToString().Contains("%load114146 = load i64, ptr %84, a"))
                {
                   // Debugger.Break();
                }

                else if(load.ToString().Contains("%load97958 = load i64, ptr %82, ali"))
                {
                   //Debugger.Break();
                }

                // Skip if it's not optimized.
                if (!instAccess.IsOptimized)
                    continue;

                // Skip if a definition already exists for this access.
                var definition = instAccess.DefiningAccess;
                if (!mssa.IsLiveOnEntryDef(definition))
                    continue;
               
                // Get the GEP.
                var gep = load.GetOperand(0);
                if (gep.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr)
                    continue;

                // Skip if this is not a binary section access.
                var gepIndex = gep.GetOperand(1);
                if (!BinaryAccessMatcher.IsConstantWithinBinarySection(gepIndex))
                    continue;

                var constant = BinaryAccessMatcher.GetBinarySectionOffset(gepIndex);
                var intWidth = load.TypeOf.IntWidth;
                var intType = LLVMTypeRef.CreateInt(intWidth);

                var size = intWidth / 8;
                var bytes = binary.ReadBytes(constant, (int)size);
                var value = size switch
                {
                    1 => bytes[0],
                    2 => BitConverter.ToUInt16(bytes),
                    4 => BitConverter.ToUInt32(bytes),
                    8 => BitConverter.ToUInt64(bytes),
                    _ => throw new InvalidOperationException()
                };

                var constInt = LLVMValueRef.CreateConstInt(intType, value);
                replacementMapping.Add(load, constInt);
            }

            if (!replacementMapping.Any())
                return false;

            foreach(var replacement in replacementMapping)
            {
                replacement.Key.ReplaceAllUsesWith(replacement.Value);
            }

            return true;
        }

    }
}
