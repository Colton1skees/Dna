using Dna.Binary;
using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.LLVMInterop.Passes.Matchers;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
            function.GlobalParent.WriteToLlFile("parent_before_structuring.ll");

            cfg = LLVMToCFG.GetCFG(function);
            domTree = new ImmutableDomTree<LLVMValueRef>(cfg);
            loopInfo = new LoopInfo(pLoopInfo);

            var mssa = new MemorySSA(m);

            var replacementMapping = new Dictionary<LLVMValueRef, LLVMValueRef>();

            foreach (var load in function.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad))
            {
                // Get the memory access.
                var instAccess = mssa.GetMemoryAccess(load);

                if (load.ToString().Contains("%load114146 = load i64, ptr %84, a"))
                {
                    // Debugger.Break();
                }

                else if (load.ToString().Contains("%load209615 = load i64, ptr %251"))
                {
                //    Debugger.Break();
                }

                // Skip if it's not optimized.
                if (!instAccess.IsOptimized)
                    continue;

                var replacement = TryResolveLoad(load, mssa, binary);
                if(replacement != null)
                    replacementMapping.Add(load, replacement.Value);
            }

            if (!replacementMapping.Any())
                return false;

            foreach(var replacement in replacementMapping)
            {
                replacement.Key.ReplaceAllUsesWith(replacement.Value);
            }

            return true;
        }

        /// <summary>
        /// Try to resolve constant loads within the binary section.
        /// </summary>
        /// <param name="loadInst"></param>
        /// <returns></returns>
        private LLVMValueRef? TryResolveLoad(LLVMValueRef loadInst, MemorySSA mssa, IBinary binary)
        {
            // Get the GEP.
            var gep = loadInst.GetOperand(0);
            if (gep.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr)
                return null;

            // Skip if this is not a binary section access.
            var gepIndex = gep.GetOperand(1);
            if (!BinaryAccessMatcher.IsConstantWithinBinarySection(gepIndex))
                return null;

            // Iteratively walk backwards while building a set of all clobbering accesses.
            OrderedSet<MemoryUseOrDef> clobberingAccesses = new();
            var initial = mssa.GetMemoryAccess(loadInst);
            MemoryAccess current = initial;
            while (true)
            {
                // If the load has a definition outside of this block(aka a memory phi),
                // we terminate and stop processing the load completely.
                if(current is MemoryPhi memoryPhi)
                {
                    return null;
                }

                // If we've reached the entry definition, then we've processed all potentially
                // clobbering stores.
                var useOrDef = (MemoryUseOrDef)current;
                if(mssa.IsLiveOnEntryDef(useOrDef))
                {
                    break;
                }

                clobberingAccesses.Add(useOrDef);

                var memoryInst = useOrDef.MemoryInst;
                if(memoryInst == null)
                {
                    Debugger.Break();
                }

                else
                {
                    current = mssa.Walker.GetClobberingMemoryAccess(memoryInst);
                }
            }

            // The initial access is just a `load`, so we discard it. 
            // A load cannot be a clobbering access.
            clobberingAccesses.Remove(initial);

            // Traverse through all clobbering stores in the order that they would execute,
            // while maintaining a list of concretized address values.
            // If at any point a *non* concrete store is identified(e.g. a store of a symbolic value),
            // then the whole routine terminates.
            Dictionary<ulong, byte> concretizedBytes = new Dictionary<ulong, byte>();
            foreach(var clobberInst in clobberingAccesses.Reverse().Select(x => x.MemoryInst))
            {
                // If the clobber is not a store, then it must be an atomic / fence, or some type of intrinsic.
                // We don't yet support this.
                if (clobberInst.InstructionOpcode != LLVMOpcode.LLVMStore)
                    throw new InvalidOperationException($"Cannot track clobber with instruction: {clobberInst}.");

                // If we are not storing a constant, then the value is unknown. We cannot safely resolve this.
                var storeValue = clobberInst.GetOperand(0);
                if (storeValue.Kind != LLVMValueKind.LLVMConstantIntValueKind)
                    return null;

                // If the store address is not a getelementptr, then it must be a global or something. 
                // This should never happen.
                var storeGep = clobberInst.GetOperand(1);
                if (storeGep.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr)
                    return null;

                // We only support analyze aliasing stores *directly* into a known binary offset.
                // This is supported:
                //      store i64 constant, [binary_section_address]
                // This is *not* supported:
                //      store i64 constant, [binary_section_address + symbolic_index]
                var storeGepIndex = storeGep.GetOperand(1);
                if (!BinaryAccessMatcher.IsConstantWithinBinarySection(storeGepIndex))
                    return null;

                var memValue = storeValue.ConstIntZExt;
                var byteWidth = storeValue.TypeOf.IntWidth / 8;
                var bytes = byteWidth switch
                {
                    1 => new byte[] { (byte)memValue },
                    2 => BitConverter.GetBytes((ushort)memValue),
                    4 => BitConverter.GetBytes((uint)memValue),
                    8 => BitConverter.GetBytes(memValue),
                    _ => throw new InvalidOperationException()
                };

                var storeOffset = BinaryAccessMatcher.GetBinarySectionOffset(storeGepIndex);
                for(ulong i = 0; i < byteWidth; i++)
                {
                    concretizedBytes[storeOffset + i] = bytes[i];
                    //concretizedBytes.Add(storeOffset + i, bytes[i]);
                }
            }

            // Create a byte array containing the concrete loaded value.
            var loadByteWidth = loadInst.TypeOf.IntWidth / 8;
            var loadedBytes = new byte[loadByteWidth];
            var loadOffset = BinaryAccessMatcher.GetBinarySectionOffset(gepIndex);
            for(ulong i = 0; i < loadByteWidth; i++)
            {
                var address = loadOffset + i;
                loadedBytes[i] = concretizedBytes.ContainsKey(address) ? concretizedBytes[address] : binary.ReadBytes(address, 1)[0];
            }

            // Create a constant ulong value.
            var resolvedConstant = loadByteWidth switch
            {
                1 => loadedBytes[0],
                2 => BitConverter.ToUInt16(loadedBytes),
                4 => BitConverter.ToUInt32(loadedBytes),
                8 => BitConverter.ToUInt64(loadedBytes),
                _ => throw new InvalidOperationException()
            };

            return LLVMValueRef.CreateConstInt(loadInst.TypeOf, resolvedConstant);
        }

    }
}
