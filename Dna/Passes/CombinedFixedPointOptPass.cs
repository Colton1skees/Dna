using Dna.Binary;
using Dna.ControlFlow;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
using Dna.LLVMInterop.API.LLVMBindings.IR;
using Dna.Passes;
using Dna.Passes.Matchers;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WebAssembly.Instructions;
using static Dna.LLVMInterop.NativePassApi;
using StoreOffsetMapping = System.Collections.Generic.Dictionary<long, (LLVMSharp.Interop.LLVMValueRef byteSource, ulong byteIndex)>;


namespace Dna.Passes
{
    public struct OpaqueSimplifyQuery
    {
        nint handle;
    }

    public class SimplifyQuery
    {
        public nint handle;

        public SimplifyQuery(nint handle)
        {
            this.handle = handle;
        }
    }


    //public record SsaValue(LLVMValueRef value);
    //public record SsaPhi(MemoryPhi memoryPhi, List<LLVMValueRef> values);

    public record SsaValue(LLVMBasicBlockRef block, LLVMValueRef value);

    public union SsaEdge(SsaValue, List<SsaEdge>);


    public class FixedpointPassConfig
    {
        // Perform adhoc instruction combining 
        public bool AdhocInstcombine = true;

        // Use LLVM's InstSimplify method to simplify individual instructions
        public bool InstSimplify = false;

        public bool ConstFold = true;

        // Maximum  on depth used during store to load elimination
        public int MaxLoadElimDepth = int.MaxValue;

        // If enabled, we only queue load instructions to the worklist initially.
        public bool VisitLoadsOnly = false;
    }

    public record BaseWithOffset(LLVMValueRef Base, ulong Offset);
    public record ClobberingStore(MemoryUseOrDef UseOrDef, BaseWithOffset BaseOffsetPair);
    public record LoadReplacement(LLVMValueRef ToReplace, LLVMValueRef Replacement);

    // This class applies multiple different optimizations at the same time in a fixed point loop.
    // Namely we implement: Store to load propagation, constant propagation, and concretizations of loads to known constant binary sections.
    // Other optimizations (dead store elimination, mba simplification) are expected to be added into the fixed point loop as we get time. 
    //
    // It's worth noting that the combined pass fixedpoint loop was necessary for performance reasons. Without this we see major phase ordering issues, 
    // because obfuscators interweave different transformations. Combining the passes improved runtime by 1-2 orders of magnitude. 
    public class CombinedFixedpointOptPass
    {
        public static int runCount = 0;

        private LLVMBuilderRef builder;

        private LLVMValueRef function;

        private MemorySSA mssa;

        private LLVMValueRef memPtr;

        private IBinary bin;

        public FixedpointPassConfig config;

        public bool Changed = false;

        public dgCombinedFixedpointPass PtrToStoreLoadPropagation { get; }

        public unsafe CombinedFixedpointOptPass(IBinary binary, FixedpointPassConfig config)
        {
            bin = binary;
            this.config = config;
            PtrToStoreLoadPropagation = new dgCombinedFixedpointPass(StoreToLoadPropagation);
        }

        private unsafe bool StoreToLoadPropagation(LLVMOpaqueValue* function, nint loopInfo, nint domTree, nint mssa, nint simplifyQuery)
        {
            builder = LLVMBuilderRef.Create(LLVMContextRef.Global);
            var r = Run(function, new LoopInfo(loopInfo), new DominatorTree(domTree), new MemorySSA(mssa), new SimplifyQuery(simplifyQuery));
            new LLVMValueRef((nint)function).GlobalParent.Verify(LLVMVerifierFailureAction.LLVMAbortProcessAction);
            return r;
        }

        private BaseWithOffset GetCanonicalBasePlusOffsetOld(LLVMValueRef current)
        {
            var currentBase = current;
            ulong currentOffset = 0;

            bool cont = true;
            while (cont)
            {
                switch (currentBase.InstructionOpcode)
                {
                    // Val = ADD (X, Const) -> (X, C + Const)
                    // Val = ADD (Const, X) -> (X, C + Const)
                    case LLVMOpcode.LLVMAdd:
                        {
                            var op0 = currentBase.GetOperand(0);
                            var op1 = currentBase.GetOperand(1);

                            Debug.Assert(!(op0.Kind == LLVMValueKind.LLVMConstantIntValueKind && op1.Kind == LLVMValueKind.LLVMConstantIntValueKind));
                            Debug.Assert(op1.TypeOf.IntWidth <= 64);

                            if (op1.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                            {
                                currentOffset += op1.ConstIntZExt;
                                currentBase = op0;
                            }
                            else if (op0.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                            {
                                currentOffset += op0.ConstIntZExt;
                                currentBase = op1;
                            }
                            else
                            {
                                cont = false;
                            }

                            break;
                        }
                    // Val = SUB (X, Const) -> (X, C - Const)
                    case LLVMOpcode.LLVMSub:
                        {
                            var op0 = currentBase.GetOperand(0);
                            var op1 = currentBase.GetOperand(1);

                            Debug.Assert(!(op0.Kind == LLVMValueKind.LLVMConstantIntValueKind && op1.Kind == LLVMValueKind.LLVMConstantIntValueKind));
                            Debug.Assert(op1.TypeOf.IntWidth <= 64);
                            if (op1.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                            {
                                currentOffset -= op1.ConstIntZExt;
                                currentBase = op0;
                            }
                            else
                            {
                                cont = false;
                            }

                            break;
                        }
                    // Val = GEP(MEM, X) -> (X, C)
                    case LLVMOpcode.LLVMGetElementPtr:
                        {
                            if (currentBase.OperandCount != 2)
                            {
                                cont = false;
                                break;
                            }

                            var op0 = currentBase.GetOperand(0);
                            var op1 = currentBase.GetOperand(1);

                            if (op0 == memPtr)
                            {
                                // continue on with new ptr
                                currentBase = op1;
                            }
                            else
                            {
                                cont = false;
                                break;
                            }

                            break;
                        }
                    default:
                        cont = false;
                        break;
                }

                if (currentBase.Kind != LLVMValueKind.LLVMInstructionValueKind)
                {
                    break;
                }
            }

            return new BaseWithOffset(currentBase, currentOffset);
        }

        private BaseWithOffset GetCanonicalBasePlusOffset(LLVMValueRef currentBase)
        {
            ulong currentOffset = 0;

            bool cont = true;

            while (cont)
            {
                switch (currentBase.InstructionOpcode)
                {
                    // Val = ADD (X, Const) -> (X, C + Const)
                    // Val = ADD (Const, X) -> (X, C + Const)
                    case LLVMOpcode.LLVMAdd:
                        {
                            var op0 = currentBase.GetOperand(0);
                            var op1 = currentBase.GetOperand(1);

                            var bothConstant = (op0.Kind == LLVMValueKind.LLVMConstantIntValueKind && op1.Kind == LLVMValueKind.LLVMConstantIntValueKind);
                            if (bothConstant)
                                return null;
                            //Debug.Assert(!bothConstant);
                            //Debug.Assert(op1.TypeOf.IntWidth <= 64);

                            if (op1.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                            {
                                currentOffset += op1.ConstIntZExt;
                                currentBase = op0;
                            }
                            else if (op0.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                            {
                                currentOffset += op0.ConstIntZExt;
                                currentBase = op1;
                            }
                            else
                            {
                                cont = false;
                            }

                            break;
                        }
                    // Val = SUB (X, Const) -> (X, C - Const)
                    case LLVMOpcode.LLVMSub:
                        {
                            var op0 = currentBase.GetOperand(0);
                            var op1 = currentBase.GetOperand(1);

                            Debug.Assert(!(op0.Kind == LLVMValueKind.LLVMConstantIntValueKind && op1.Kind == LLVMValueKind.LLVMConstantIntValueKind));
                            Debug.Assert(op1.TypeOf.IntWidth <= 64);
                            if (op1.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                            {
                                currentOffset -= op1.ConstIntZExt;
                                currentBase = op0;
                            }
                            else
                            {
                                cont = false;
                            }

                            break;
                        }
                    // Val = GEP(MEM, X) -> (X, C)
                    case LLVMOpcode.LLVMGetElementPtr:
                        {
                            if (currentBase.OperandCount != 2)
                            {
                                cont = false;
                                break;
                            }

                            var pointerOperand = currentBase.GetOperand(0);
                            var indexOperand = currentBase.GetOperand(1);

                            // getelementptr ..., ptr %memory, i64 %index
                            // Canonical representation: (%index, 0).
                            if (pointerOperand == memPtr)
                            {
                                currentBase = indexOperand;
                                break;
                            }

                            // getelementptr ..., ptr (getelementptr ...), i64 C
                            // Fold a constant outer displacement into the accumulated offset.
                            if (pointerOperand.Is(LLVMOpcode.LLVMGetElementPtr) &&
                                indexOperand.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                            {
                                currentOffset += indexOperand.ConstIntZExt;
                                currentBase = pointerOperand;
                                break;
                            }

                            cont = false;
                            break;
                        }
                    // Val = FREEZE(X) -> (X, C)
                    // A freeze only pins down the value of a poison operand. If the operand is poison then any
                    // access through this pointer is UB regardless, so the canonical base is the frozen operand.
                    case LLVMOpcode.LLVMFreeze:
                        {
                            currentBase = currentBase.GetOperand(0);
                            break;
                        }
                    default:
                        cont = false;
                        break;
                }

                if (currentBase.Kind != LLVMValueKind.LLVMInstructionValueKind)
                {
                    break;
                }
            }

            return new BaseWithOffset(currentBase, currentOffset);
        }

        private bool Run(LLVMValueRef function, LoopInfo loopInfo, DominatorTree domTree, MemorySSA mssa, SimplifyQuery simplifyQuery)
        {
            this.function = function;
            this.mssa = mssa;

            var globalMemPtr = function.GlobalParent.GetGlobals().First(x => x.Name.Contains("memory"));
            this.memPtr = function.EntryBasicBlock.GetInstructions().SingleOrDefault(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad && x.GetOperand(0) == globalMemPtr);

            // Process each instruction.
            int numIterations = 0;
            using (var updater = new MemorySSAUpdater(mssa))
            {
                var localChanged = true;
                //while (localChanged && numIterations < 10)
                while (numIterations == 0) // we don't need to loop anymore because this *should* converge in a single iteration
                {
                    numIterations++;
                    localChanged = false;

                    var instcombine = new AdhocInstCombinePass();
                    instcombine.builder = LLVMBuilderRef.Create(function.GetFunctionCtx());

                    var rpoInstructions = LLVMUtil.GetRpoInstructions(function);
                    WorkList<LLVMValueRef> worklist = new();
                    if (!config.VisitLoadsOnly)
                        worklist.AddRangeToBack(rpoInstructions);
                    else
                        worklist.AddRangeToBack(rpoInstructions.Where(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad));
                   

                    while (worklist.Count > 0)
                    {
                        var nextInstr = worklist.PopFront();


                        if (!nextInstr.Is(LLVMValueKind.LLVMInstructionValueKind))
                            continue;

                        //var isTgt = nextInstr.Is(LLVMOpcode.LLVMLoad) && nextInstr.GetOperand(0).Is(LLVMOpcode.LLVMGetElementPtr) && nextInstr.GetOperand(0).GetOperand(0).ToString().Contains("i64 96");
                        //if (!isTgt)
                        //    continue;

                        // Delete trivially dead instructions
                        if (false && ConstantFoldingAPI.IsInstructionTriviallyDead(nextInstr))
                        {
                            var memoryAccess = mssa.GetMemoryAccess(nextInstr);
                            if (memoryAccess.Handle != 0)
                                updater.RemoveMemoryAccess(nextInstr);

                            nextInstr.InstructionEraseFromParent();
                            continue;
                        }

                        var opc = nextInstr.InstructionOpcode;
               
                        bool TryReplaceAndRemove(LLVMValueRef replaceWith)
                        {
                            if (replaceWith.Handle == 0 || replaceWith.Handle == nextInstr.Handle)
                                return false;

                            var memoryAccess = mssa.GetMemoryAccess(nextInstr);
                            if (memoryAccess.Handle != 0)
                                updater.RemoveMemoryAccess(nextInstr);

                            var users = nextInstr.GetUsers().Where(sel => sel.Handle != nextInstr.Handle).ToList();
                            worklist.AddRangeToFront(users);

                            nextInstr.ReplaceAllUsesWith(replaceWith);
                            nextInstr.InstructionEraseFromParent();

                            localChanged = true;
                            return true;
                        }

                        bool TryReplaceAndRemove2(PeepholeResult peephole)
                        {
                            if (peephole == null)
                                return false;

                            var replacement = peephole.GetResult();
                            if (replacement.Handle == 0 || replacement.Handle == nextInstr.Handle)
                                return false;

                            if (peephole.Insts.Any(x => x.Is(LLVMValueKind.LLVMInstructionValueKind) && x.GetOperands().Contains(nextInstr)))
                                return false;

                            //// Append all newly created instructions to the worklist, such that we visit them in RPO order(%t0, then %t1)
                            //// %t0 = add x,y
                            //// %t1 = mul %t0, 111
                            foreach (var reversed in peephole.Insts.Where(x => x.Handle != nextInstr.Handle).Reverse<LLVMValueRef>())
                                worklist.AddToFront(reversed);

                            // Replace and remove the old input inst
                            return TryReplaceAndRemove(replacement);

                        }

                        // Do a const-prop loop before anything else since we don't want to do redundant work.
                        if (config.ConstFold)
                        {
                            var result = ConstantFoldingAPI.TryConstantFold(nextInstr);
                            if (result != null)
                            {
                                Debug.Assert(!worklist.Contains(nextInstr));
                                TryReplaceAndRemove(result);
                                continue;
                            }
                        }

                        if (config.InstSimplify && opc != LLVMOpcode.LLVMLoad && opc != LLVMOpcode.LLVMStore)
                        {
                            var simplified = ConstantFoldingAPI.TrySimplify(nextInstr);
                            if (simplified != null)
                            {
                                TryReplaceAndRemove(simplified);
                                continue;
                            }
                        }

                        if (config.AdhocInstcombine)
                        {
                        
                            var peephole = instcombine.PeepholeInst(nextInstr, domTree, simplifyQuery);
                            if (TryReplaceAndRemove2(peephole))
                                continue;
                        }


                        if (opc == LLVMOpcode.LLVMLoad)
                        {
                            var repl = ProcessLoad(nextInstr, updater, 0);
                            if (repl != null)
                            {
                                TryReplaceAndRemove2(repl);
                                continue;
                            }
                        }

                        if (Hoist(nextInstr, opc, domTree))
                        {
                            var users = nextInstr.GetUsers().Where(sel => sel.Handle != nextInstr.Handle).ToList();
                            worklist.AddRangeToFront(users);

                            localChanged = true;
                            continue;
                        }


                    }

                    Changed |= localChanged;
                }
            }

            mssa.Validate();

            Console.WriteLine($"Worklist converged in {numIterations} iterations!");
            return true;
        }


        private bool Hoist(LLVMValueRef inst, LLVMOpcode opcode, DominatorTree domTree)
        {
            return false;
            var matches = opcode == LLVMOpcode.LLVMAdd || opcode == LLVMOpcode.LLVMSub || opcode == LLVMOpcode.LLVMGetElementPtr;
            if (!matches)
                return false;
            if (opcode == LLVMOpcode.LLVMGetElementPtr && inst.OperandCount != 2)
                return false;

            var operands = inst.GetOperands().Where(x => x.Is(LLVMValueKind.LLVMInstructionValueKind)).ToList();
            bool AvailableIn(LLVMBasicBlockRef block) => operands.All(x =>
                x.InstructionParent == block ? x != block.Terminator : domTree.Dominates(x, block));

            var target = function.EntryBasicBlock;
            if (!AvailableIn(target))
                target = operands.Select(x => x.InstructionParent).FirstOrDefault(AvailableIn);

            if (target.Handle == 0 || !domTree.ProperlyDominates(target, inst.InstructionParent))
                return false;

            LLVM.InstructionRemoveFromParent(inst);
            builder.PositionBefore(target.Terminator);
            builder.Insert(inst);
            inst.GetFunction().GlobalParent.PrintToFile("translatedFunction.ll");
            return true;
        }

        private bool IsValidMemoryAccess(MemoryAccess memAccess)
        {
            // If we reach a memory phi then we stop iteratively walking backward.
            // Some kind of multi-block analysis would be required in this scenario.
            if (memAccess is MemoryPhi)
                return false;

            // If we've reached the entry definition(aka the point where there could not possibly be any clobbering stores that
            // precede this memory access), then we cannot backtrack any further.
            var useOrDef = (MemoryUseOrDef)memAccess;
            if (mssa.IsLiveOnEntryDef(useOrDef))
                return false;

            return true;
        }
        private uint LoadSizeOf(LLVMTypeRef type)
        {
            switch (type.Kind)
            {
                case LLVMTypeKind.LLVMIntegerTypeKind:
                    Debug.Assert(type.IntWidth % 8 == 0);
                    return type.IntWidth / 8;
                case LLVMTypeKind.LLVMVectorTypeKind:
                    Debug.Assert(type.ElementType.Kind == LLVMTypeKind.LLVMIntegerTypeKind);
                    return type.VectorSize * (type.ElementType.IntWidth / 8);
                default:
                    throw new Exception("unsupported");
            }
        }

        public record ByteSource(LLVMValueRef src, byte index);

        private ByteSource? Process(MemoryUseOrDef firstAccess, BaseWithOffset loadBaseAndOffset, MemoryAccess currAccess, uint loadSize, int loadIndex, HashSet<LLVMBasicBlockRef> visitedPhis)
        {
            if (currAccess is MemoryPhi memoryPhi)
            {
                if (visitedPhis.Contains(memoryPhi.Block))
                    return null;

                visitedPhis.Add(memoryPhi.Block);

                // Backtrack to find a definition
                List<ByteSource> incomingValues = new();
                foreach (var incomingAccess in memoryPhi.IncomingMemoryAccesses)
                {
                    var value = Process(firstAccess, loadBaseAndOffset, incomingAccess, loadSize, loadIndex, visitedPhis);
                    // Bail if we failed to find a definition
                    if (value == null)
                        return null;

                    // Bail if we find multiple conflicts definitions.
                    if (incomingValues.Count > 0 && value != incomingValues[0])
                        return null;

                    incomingValues.Add(value);
                }

                return incomingValues[0];

                // Otherwise we found a definition along all paths..
            }

            // Break out of the loop if we hit a memory clobber we can't handle.
            if (!IsValidMemoryAccess(currAccess))
                return null;

            var newAccess = (MemoryUseOrDef)currAccess;

            var skip = () => Process(firstAccess, loadBaseAndOffset, newAccess.DefiningAccess, loadSize, loadIndex, visitedPhis);

            // If the new clobber doesn't alias the original clobber, continue on.
            // TODO: MayAlias may be wrong in the presence of loops? Need to refer to DeadStoreElimination impl
            if (!mssa.MayAlias(firstAccess, newAccess))
                return skip();


            // This should never happen.
            var memoryInst = newAccess.MemoryInst;
            if (memoryInst == null)
            {
                Debugger.Break();
                throw new InvalidOperationException($"No defining memory instruction!");
            }

            // If the clobber is not a store, then it must be an atomic / fence, or some type of intrinsic.
            // We don't yet support this.
            if (memoryInst.InstructionOpcode != LLVMOpcode.LLVMStore)
            {
                // Special-case out soteria_error, we don't care about it.
                if (memoryInst.InstructionOpcode == LLVMOpcode.LLVMCall && memoryInst.GetOperand(0).Kind == LLVMValueKind.LLVMFunctionValueKind && memoryInst.GetOperand(0).Name == "soteria_error")
                    return skip();

                throw new InvalidOperationException($"Cannot track clobber with instruction: {memoryInst}.");
            }

            // Bail if we get a base mismatch, we can't make any aliasing guarantees here.
            var newBaseAndOffset = GetCanonicalBasePlusOffset(memoryInst.GetOperand(1));
            if (loadBaseAndOffset.Base != newBaseAndOffset.Base)
            {
                var ignore = newBaseAndOffset.Base.Kind == LLVMValueKind.LLVMGlobalVariableValueKind;
                if (ignore)
                    return skip();

                return null;
            }


            var storeVal = memoryInst.GetOperand(0);
            var storeSize = LoadSizeOf(storeVal.TypeOf);

            var loadOffset = loadBaseAndOffset.Offset;
            // Make everything relative to the load and convert to signed integers.
            var storeStart = Math.Max((long)(newBaseAndOffset.Offset - loadOffset), 0);
            var storeEnd = Math.Min((long)((newBaseAndOffset.Offset + (storeSize - 1)) - loadOffset), loadSize - 1);

            // Skip this definition unless it provides the particular byte being resolved.
            if (storeEnd < 0 ||
                storeStart >= loadSize ||
                loadIndex < storeStart ||
                loadIndex > storeEnd)
            {
                //Console.WriteLine($" --> Ignoring dead store: ([{storeStart}, {storeEnd}] does not overlap with load");
                return skip();
            }

            long initialOffset = 0;
            // Otherwise they overlap
            if (loadOffset < newBaseAndOffset.Offset)
            {
                var foo1 = (long)loadOffset;
                var foo2 = (long)newBaseAndOffset.Offset;
                // You must take the load starting point and then add the store offset.
                // Note that the store offset is relative to the load. So the store offset for the example above would be one - giving you
                initialOffset = 0 - storeStart;
            }

            /*
            store:
                0 = foo
                1 = foo
                2 = foo
                3 = foo

            load(1,2,3,4)
            load offset comes after the store

            loadOffset = 1
            newBaseAndOffset = 0
            */
            else if (loadOffset > newBaseAndOffset.Offset)
            {
                var foo3 = (long)loadOffset;
                var foo4 = (long)newBaseAndOffset.Offset;
                // Subtract the load starting offset from the store start starting offset.
                // Giving you -1 here as the initial offset. Then e.g. if we are handling the load offset 
                initialOffset = (long)(loadOffset - newBaseAndOffset.Offset);
            }

            initialOffset += loadIndex;

            // Constant fold
            if (storeVal.IsConstant())
            {
                var bytes = BitConverter.GetBytes(storeVal.ConstIntZExt);
                var value = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, bytes[initialOffset]);
                storeVal = value;
                initialOffset = 0;
            }

            //initialOffset += unhandledIndex;
            //handledBytesToValue.Add(unhandledIndex, (storeVal, (byte)(initialOffset)));
            return new ByteSource(storeVal, (byte)initialOffset);
        }


        private PeepholeResult ProcessLoad(LLVMValueRef loadInst, MemorySSAUpdater updater, int depth)
        {
           
            /*
            if (loadInst.ToString().Contains("%292 = load i64, ptr %71"))
            {
                loadInst.GetFunction().GlobalParent.PrintToFile("translatedFunction.ll");
                Debugger.Break();

            }
      
            
            if (loadInst.GetOperand(0).Is(LLVMOpcode.LLVMGetElementPtr) && loadInst.GetOperand(0).OperandCount == 2 && loadInst.GetOperand(0).GetOperand(1).IsConstant(0xFFFFFFFFFFFFFF70))
            {
                loadInst.GetFunction().GlobalParent.PrintToFile("translatedFunction.ll");
                Debugger.Break();
            }
            */


      

            // Exit early if we hit the max recursion depth
            if (depth >= config.MaxLoadElimDepth)
                return null;



            var repl = ProcessBinaryLoad(loadInst, updater);
            if (repl != null)
            {
                return repl;
            }


            if (loadInst.TypeOf.Kind != LLVMTypeKind.LLVMIntegerTypeKind && loadInst.TypeOf.Kind != LLVMTypeKind.LLVMPointerTypeKind)
            {
                Debugger.Break();
            }

            if (loadInst.TypeOf.Kind != LLVMTypeKind.LLVMIntegerTypeKind)
                return null;

            // Get the initial memory access from the load. Bail if we can't do anything with it.
            var firstAccess = mssa.GetMemoryAccess(loadInst);
            if (!IsValidMemoryAccess(firstAccess))
                return null;

            var loadSize = LoadSizeOf(loadInst.TypeOf);
            Debug.Assert(loadSize <= 8);

            var loadBaseAndOffset = GetCanonicalBasePlusOffset(loadInst.GetOperand(0));
            if (loadBaseAndOffset == null)
                return null;
            var loadOffset = loadBaseAndOffset.Offset;


            // For each loaded byte, keep track of:
            //  (a) - the LLVM value that is being stored at that load offset
            //  (b) - the byte index of the LLVM value that needs to be fetched
            StoreOffsetMapping handledBytesToValue = new();

            var current = firstAccess;

            /*
            if (loadInst.ToString().Contains("load i64, ptr %88,"))
            {
                function.GlobalParent.PrintToFile("translatedFunction.ll");
                Debugger.Break();
                var bar = Process(firstAccess, loadBaseAndOffset, current.DefiningAccess, loadSize, 0, new());

                Debugger.Break();
            }
            */

            //List<ByteSource> values = new();
            StoreOffsetMapping values = new();


            /*
            for (int i = 0; i < loadSize; i++)
            {
                var result = Process(firstAccess, loadBaseAndOffset, current.DefiningAccess, loadSize, i, new());
                if (result == null)
                    break;
                values[i] = (result.src, result.index);
            }

            if (values.Count == loadSize)
            {
                //Debugger.Break();
                return CreateFullReplacementOfLoad(loadInst, values);
            }
            */



            while (true)
            {
                // Locate the first memory write *before* our current definition that may clobber the current definition.
                // Alternatively this may return a MemoryPhi or a "LiveOnEntry" object.
                var newAccess = current.DefiningAccess;

                // Break out of the loop if we hit a memory clobber we can't handle.
                if (!IsValidMemoryAccess(newAccess))
                    break;

                current = (MemoryUseOrDef)newAccess;

                // If the new clobber doesn't alias the original clobber, continue on.
                if (!mssa.MayAlias(firstAccess, current))
                    continue;

                // This should never happen.
                var memoryInst = current.MemoryInst;
                if (memoryInst == null)
                {
                    Debugger.Break();
                    throw new InvalidOperationException($"No defining memory instruction!");
                }

                // If the clobber is not a store, then it must be an atomic / fence, or some type of intrinsic.
                // We don't yet support this.
                if (memoryInst.InstructionOpcode != LLVMOpcode.LLVMStore)
                {
                    // Special-case out soteria_error, we don't care about it.
                    if (memoryInst.InstructionOpcode == LLVMOpcode.LLVMCall && memoryInst.GetOperand(0).Kind == LLVMValueKind.LLVMFunctionValueKind && memoryInst.GetOperand(0).Name == "soteria_error")
                        continue;

                    throw new InvalidOperationException($"Cannot track clobber with instruction: {memoryInst}.");
                }

                // Bail if we get a base mismatch, we can't make any aliasing guarantees here.
                var newBaseAndOffset = GetCanonicalBasePlusOffset(memoryInst.GetOperand(1));
                if (loadBaseAndOffset.Base != newBaseAndOffset.Base)
                {
                    var ignore = newBaseAndOffset.Base.Kind == LLVMValueKind.LLVMGlobalVariableValueKind;
                    if (ignore)
                        continue;


                    break;
                }

                var storeVal = memoryInst.GetOperand(0);
                var storeSize = LoadSizeOf(storeVal.TypeOf);

                // Make everything relative to the load and convert to signed integers.
                var storeStart = Math.Max((long)(newBaseAndOffset.Offset - loadOffset), 0);
                var storeEnd = Math.Min((long)((newBaseAndOffset.Offset + (storeSize - 1)) - loadOffset), loadSize - 1);

                // If storeEnd is < 0 or storeStart >= loadSize we can do an early bail.
                if (storeEnd < 0 || storeStart > loadSize)
                {
                    //Console.WriteLine($" --> Ignoring dead store: ([{storeStart}, {storeEnd}] does not overlap with load");
                    continue;
                }

                //If we've already handled all of the bytes taken from this store, ignore it.
                var unhandledIndices = GetUnhandledLoadIndices(storeStart, storeEnd, handledBytesToValue);
                if (!unhandledIndices.Any())
                    continue;

                if (storeVal.TypeOf.Kind != LLVMTypeKind.LLVMIntegerTypeKind)
                {
                    Debugger.Break();
                }

                foreach (var unhandledIndex in unhandledIndices)
                {
                    long initialOffset = 0;

                    /*
                     store:
	                    1 = foo
	                    2 = foo
	                    3 = foo

                    load(0,1,2,3)
                    load offset comes before the store(newBaseAndOffset.Offset) offset
                    // loadOffset = 0
                    // storeOffset = 1
                    // storeStart = 1
                    // initialOffset = -1
                    */
                    if (loadOffset < newBaseAndOffset.Offset)
                    {
                        var foo1 = (long)loadOffset;
                        var foo2 = (long)newBaseAndOffset.Offset;
                        // You must take the load starting point and then add the store offset.
                        // Note that the store offset is relative to the load. So the store offset for the example above would be one - giving you
                        initialOffset = 0 - storeStart;
                    }

                    /*
                    store:
	                    0 = foo
	                    1 = foo
	                    2 = foo
	                    3 = foo
	
                    load(1,2,3,4)
                    load offset comes after the store

                    loadOffset = 1
                    newBaseAndOffset = 0
                    */
                    else if (loadOffset > newBaseAndOffset.Offset)
                    {
                        var foo3 = (long)loadOffset;
                        var foo4 = (long)newBaseAndOffset.Offset;
                        // Subtract the load starting offset from the store start starting offset.
                        // Giving you -1 here as the initial offset. Then e.g. if we are handling the load offset 
                        initialOffset = (long)(loadOffset - newBaseAndOffset.Offset);
                    }

                    initialOffset += unhandledIndex;
                    handledBytesToValue.Add(unhandledIndex, (storeVal, (byte)(initialOffset)));
                }

                if (handledBytesToValue.Count == loadSize)
                {
                    Console.WriteLine($"   --> Final store found! Bailing out at depth {depth}.");
                    break;
                }
            }

            // If all bytes of the load are known:
            if (handledBytesToValue.Count == loadSize)
            {
                bool TryFoldByteSources(StoreOffsetMapping byteSources, out ulong constant)
                {
                    constant = 0;

                    if (byteSources.Count != loadSize)
                        return false;

                    for (long byteOffset = 0; byteOffset < loadSize; byteOffset++)
                    {
                        if (!byteSources.TryGetValue(byteOffset, out var source) ||
                            !source.byteSource.IsConstant() ||
                            source.byteIndex >= sizeof(ulong))
                        {
                            return false;
                        }

                        var sourceByte = (source.byteSource.ConstIntZExt >> (8 * (int)source.byteIndex)) & 0xff;
                        constant |= sourceByte << (8 * (int)byteOffset);
                    }

                    return true;
                }

                if (values.Count == loadSize &&
                    TryFoldByteSources(values, out var recursiveValue) &&
                    TryFoldByteSources(handledBytesToValue, out var iterativeValue) &&
                    recursiveValue != iterativeValue)
                {
                    Debugger.Break();
                }
    

                return CreateFullReplacementOfLoad(loadInst, handledBytesToValue);
            }

            // Partially known load - we then truncate the size of the load!
            else if (handledBytesToValue.Count > 0)
            {
                // Reduce the size of the load if we can do so with a power of two.
                Console.WriteLine($"Encountered partially known load for {loadInst}. {handledBytesToValue.Count}/{loadSize} are known. You can reduce the size of the load using this knowledge.");
                // Debugger.Break();
            }
            
            else
            {
                LLVMValueRef selectCond;
                LLVMValueRef ptr1;
                LLVMValueRef ptr2;

                var loadPtr = loadInst.GetOperand(0);
                var baseWithConstantSelect = KnownIndexStoreToLoadPropagation.GetAsBaseWithConstantSelect(loadInst);
                if (baseWithConstantSelect != null)
                {
                    builder.PositionBefore(loadInst);

                    var index1 = builder.BuildAdd(baseWithConstantSelect.BasePtr, baseWithConstantSelect.SelectOfTwoConstantIndices.GetOperand(1));
                    var index2 = builder.BuildAdd(baseWithConstantSelect.BasePtr, baseWithConstantSelect.SelectOfTwoConstantIndices.GetOperand(2));
                    var ptrTy = function.GetFunctionCtx().GetPtrType();
                    ptr1 = builder.BuildGEP2(ptrTy, memPtr, new LLVMValueRef[] { index1 });
                    ptr2 = builder.BuildGEP2(ptrTy, memPtr, new LLVMValueRef[] { index2 });
                    selectCond = baseWithConstantSelect.SelectOfTwoConstantIndices.GetOperand(0);
                }
                else if (loadPtr.Is(LLVMOpcode.LLVMSelect) &&
                    loadPtr.GetOperand(1).TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind &&
                    loadPtr.GetOperand(2).TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
                {
                    builder.PositionBefore(loadInst);
                    selectCond = loadPtr.GetOperand(0);
                    ptr1 = loadPtr.GetOperand(1);
                    ptr2 = loadPtr.GetOperand(2);
                }
                else
                {
                    return null;
                }

                // Create the loads
                LLVMValueRef load1 = null;
                LLVMValueRef load2 = null;
       
                
                load1 = builder.BuildLoad2(loadInst.TypeOf, ptr1);
                load2 = builder.BuildLoad2(loadInst.TypeOf, ptr2);

                // Update MSSA to be aware of the second load.
                // Note that we set the insert point to be right before the original load.
                var insertPoint2 = mssa.GetMemoryAccess(loadInst);
                var mssaLoad2 = updater.CreateMemoryAccessBefore(load2, null, insertPoint2);
                updater.InsertUse(mssaLoad2, false);

                // Update MSSA to be aware of the first load.
                // Note that because the loads are in the order of "load1, load2, original" (and we only have the createbefore api implemented),
                // we set the insert point of the first load to be before the second load.
                var insertPoint1 = mssa.GetMemoryAccess(load2);
                var mssaLoad1 = updater.CreateMemoryAccessBefore(load1, null, insertPoint1);
                updater.InsertUse(mssaLoad1, false);
                

                var solution1 = ProcessLoad(load1, updater, depth + 1);
                if(solution1 == null)
                {
                    Console.WriteLine("Bailing out: First memory location could not be resolved to another load.");
                    return null;
                }

                // Try to solve the second load.
                var solution2 = ProcessLoad(load2, updater, depth + 1);
                if (solution2 == null)
                {
                    Console.WriteLine("Bailing out: Second memory location could not be resolved to another load.");
                    return null;
                }

                // Ok, we were able to propagate both load destinations to other loads.
                var combined = builder.BuildSelect(selectCond, solution1.GetResult(), solution2.GetResult());


                var peephole = new PeepholeResult();
                peephole.Add(solution1.Insts);
                peephole.Add(solution2.Insts);
                peephole.Add(combined);
                return peephole;
            }

    

            return null;
        }

        // Get a list of indices for each load byte that has not yet been accounted for.
        private IReadOnlyList<long> GetUnhandledLoadIndices(long storeStart, long storeEnd, StoreOffsetMapping handledBytesToValue)
        {
            var results = new List<long>();
            for (long i = storeStart; i <= storeEnd; i++)
            {
                if (!handledBytesToValue.ContainsKey(i))
                {
                    results.Add(i);
                }
            }

            return results;
        }

        private PeepholeResult CreateFullReplacementOfLoad(LLVMValueRef loadInst, StoreOffsetMapping storeOffsetMapping)
        {
            // Position the builder immediately before the load instruction.
            builder.PositionBefore(loadInst);

            // Sort the bytes by their index.
            var ordered = storeOffsetMapping.OrderBy(x => x.Key);

            var destTy = loadInst.TypeOf;
            LLVMValueRef last = LLVMValueRef.CreateConstInt(destTy, 0);
            var byteType = LLVMTypeRef.Int8;
            var peephole = new PeepholeResult();
            foreach (var handledByte in storeOffsetMapping.OrderBy(x => x.Key))
            {
                // Get the LLVMValueRef that this byte is coming form.
                var srcValue = handledByte.Value.byteSource;

                // Shift the isolated byte down to the very bottom byte position.
                var lshrBy = LLVMValueRef.CreateConstInt(srcValue.TypeOf, 8 * (handledByte.Value.byteIndex));
                var shifted = builder.BuildLShr(srcValue, lshrBy);
                peephole.Add(shifted);

                // Isolate out only the last byte.
                var byteMask = LLVMValueRef.CreateConstInt(srcValue.TypeOf, 255);
                var isolated = builder.BuildAnd(shifted, byteMask);
                peephole.Add(isolated);
                isolated = builder.BuildTrunc(isolated, byteType);
                peephole.Add(isolated);

                // Upcast the value to the size of the load.
                isolated = builder.BuildZExt(isolated, loadInst.TypeOf);
                peephole.Add(isolated);

                // Left shift the isolated byte back into it's correct position(relative to the load).
                var shlBy = LLVMValueRef.CreateConstInt(destTy, 8 * (ulong)(handledByte.Key));
                var orMask = builder.BuildShl(isolated, shlBy);
                peephole.Add(orMask);

                // Bitwise OR the isolated byte back into the target.
                last = builder.BuildOr(last, orMask);
                peephole.Add(last);
            }

            return peephole;
        }

        private static bool IsMemory(LLVMValueRef gep)
        {
            if (!gep.Is(LLVMOpcode.LLVMGetElementPtr))
                return false;
            if (gep.OperandCount != 2)
                return false;

            var load = gep.GetOperand(0);
            if (gep.OperandCount != 2)
                return false;

            var isMemGep = load.Is(LLVMOpcode.LLVMLoad) && load.GetOperand(0).Kind == LLVMValueKind.LLVMGlobalVariableValueKind;
            return isMemGep;
        }

        private PeepholeResult ProcessBinaryLoad(LLVMValueRef loadInst, MemorySSAUpdater updater)
        {
            // Type check
            if (loadInst.TypeOf.Kind != LLVMTypeKind.LLVMIntegerTypeKind)
                return null;

            // Get the GEP.
            var gep = loadInst.GetOperand(0);
            if (gep.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr)
                return null;

            var gep0 = gep.GetOperand(0);
            if (gep.OperandCount != 2)
                return null;

            // If this is not a binary section access, do a last-ditch attempt with a load-of-select.
            if (!BinaryAccessMatcher.IsConstantWithinBinarySection(bin, gep.GetOperand(1)))
                return TryProcessAsLoadOfTwoPossibleAddresses(gep, loadInst, updater);


            //if (!gep0.Is(LLVMOpcode.LLVMLoad) || gep0.GetOperand(0).Kind != LLVMValueKind.LLVMGlobalVariableValueKind)
            if (!IsMemory(gep))
            {
                //loadInst.GetFunction().GlobalParent.PrintToFile("translatedFunction.ll");
                return null;

            }
            //Console.WriteLine(gep);

            var bitWidth = loadInst.TypeOf.IntWidth;

            // Get the binary section offset.
            var sectionOffset = BinaryAccessMatcher.GetBinarySectionOffset(bin, gep.GetOperand(1));

            List<ulong> words = new();
            ulong currentWord = 0;
            int currentIndex = 0;

            var byteWidth = bitWidth / 8;
            for (ulong i = 0; i < byteWidth + (bitWidth % 8 == 0 ? 0UL : 1UL); i++)
            {
                var offset = i + sectionOffset;
                currentWord |= (ulong)bin.ReadByte(offset) << (currentIndex * 8);

                currentIndex++;
                if (currentIndex == 8)
                {
                    words.Add(currentWord);
                    currentIndex = 0;
                }
            }

            if (currentIndex != 0)
            {
                words.Add(currentWord);
            }

            Debug.Assert(words.Count != 0);

            var constantInt = LLVMValueRef.CreateConstIntOfArbitraryPrecision(loadInst.TypeOf, words.ToArray());
            var peephole = new PeepholeResult();
            peephole.Add(constantInt);
            return peephole;
        }

        private PeepholeResult TryProcessAsLoadOfTwoPossibleAddresses(LLVMValueRef gep, LLVMValueRef loadInst, MemorySSAUpdater updater)
        {
            // If this is not a select, return.
            var selectPtr = gep.GetOperand(1);
            if (selectPtr.Kind != LLVMValueKind.LLVMInstructionValueKind || selectPtr.InstructionOpcode != LLVMOpcode.LLVMSelect)
                return null;

            // If this is not a select of two binary section ptrs, return null.
            var op1 = selectPtr.GetOperand(1);
            var op2 = selectPtr.GetOperand(2);
            if (!BinaryAccessMatcher.IsConstantWithinBinarySection(bin, op1) || !BinaryAccessMatcher.IsConstantWithinBinarySection(bin, op2))
                return null;

            var peephole = new PeepholeResult();
            builder.PositionBefore(loadInst);

            // Construct loads for both constant sections.
            var ptrTy = function.GetFunctionCtx().GetPtrType();
            var gep1 = builder.BuildGEP2(ptrTy, gep.GetOperand(0), new LLVMValueRef[] { op1 });
            var gep2 = builder.BuildGEP2(ptrTy, gep.GetOperand(0), new LLVMValueRef[] { op2 });
            var load1 = builder.BuildLoad2(loadInst.TypeOf, gep1);
            var load2 = builder.BuildLoad2(loadInst.TypeOf, gep2);


            // Update MSSA to be aware of the second load.
            // Note that we set the insert point to be right before the original load.
            var insertPoint2 = mssa.GetMemoryAccess(loadInst);
            var mssaLoad2 = updater.CreateMemoryAccessBefore(load2, null, insertPoint2);
            updater.InsertUse(mssaLoad2, false);


            // Update MSSA to be aware of the first load.
            // Note that because the loads are in the order of "load1, load2, original" (and we only have the createbefore api implemented),
            // we set the insert point of the first load to be before the second load.
            var insertPoint1 = mssa.GetMemoryAccess(load2);
            var mssaLoad1 = updater.CreateMemoryAccessBefore(load1, null, insertPoint1);
            updater.InsertUse(mssaLoad1, false);


            var select = builder.BuildSelect(selectPtr.GetOperand(0), load1, load2);
            peephole.Add(gep1, gep2, load1, load2, select);
            return peephole;
        }
    }
}
