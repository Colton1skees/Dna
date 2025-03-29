using Dna.Binary;
using Dna.ControlFlow;
using Dna.DataStructures;
using Dna.Extensions;
using Dna.LLVMInterop.API.LLVMBindings.Analysis;
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Dna.LLVMInterop.NativePassApi;
using StoreOffsetMapping = System.Collections.Generic.Dictionary<long, (LLVMSharp.Interop.LLVMValueRef byteSource, ulong byteIndex)>;

namespace Dna.Passes
{
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

        public bool Changed = false;

        public dgCombinedFixedpointPass PtrToStoreLoadPropagation { get; }

        public unsafe CombinedFixedpointOptPass(IBinary binary)
        {
            bin = binary;
            PtrToStoreLoadPropagation = new dgCombinedFixedpointPass(StoreToLoadPropagation);
        }

        private unsafe bool StoreToLoadPropagation(LLVMOpaqueValue* function, nint loopInfo, nint mssa)
        {
            builder = LLVMBuilderRef.Create(LLVMContextRef.Global);
            return Run(function, new LoopInfo(loopInfo), new MemorySSA(mssa));
        }

        private BaseWithOffset GetCanonicalBasePlusOffset(LLVMValueRef current)
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

        private bool Run(LLVMValueRef function, LoopInfo loopInfo, MemorySSA mssa)
        {
            this.function = function;
            this.mssa = mssa;

            var globalMemPtr = function.GlobalParent.GetGlobals().First(x => x.Name.Contains("memory"));
            this.memPtr = function.EntryBasicBlock.GetInstructions().SingleOrDefault(x => x.InstructionOpcode == LLVMOpcode.LLVMLoad && x.GetOperand(0) == globalMemPtr);

            // Process each instruction.
            using (var updater = new MemorySSAUpdater(mssa))
            {
                var localChanged = true;
                while (localChanged)
                {
                    localChanged = false;

                    var worklist = new WorkList<LLVMValueRef>(function.GetInstructions());
                    while (worklist.Count > 0)
                    {
                        var nextInstr = worklist.PopBack();

                        void DoReplaceAndRemove(LLVMValueRef replaceWith)
                        {
                            Debug.Assert(replaceWith != nextInstr);

                            updater.RemoveMemoryAccess(nextInstr);
                            worklist.AddRangeToFront(nextInstr.GetUsers().Where(sel => sel.Handle != nextInstr.Handle));

                            nextInstr.ReplaceAllUsesWith(replaceWith);
                            nextInstr.InstructionEraseFromParent();

                            localChanged = true;
                        }

                        // Do a const-prop loop before anything else since we don't want to do redundant work.
                        unsafe
                        {
                            var result = NativeConstantFoldingAPI.TryConstantFold((LLVMOpaqueValue*)nextInstr.Handle);
                            if (result != null)
                            {
                                DoReplaceAndRemove(new LLVMValueRef((nint)result));
                                continue;
                            }
                        }

                        if (nextInstr.InstructionOpcode == LLVMOpcode.LLVMLoad)
                        {
                            var repl = ProcessLoad(nextInstr);
                            if (repl != null)
                            {
                                DoReplaceAndRemove(repl);
                                continue;
                            }
                        }
                    }

                    Changed |= localChanged;
                }
            }

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

        private LLVMValueRef ProcessLoad(LLVMValueRef loadInst)
        {
            /*
            if(loadInst.ToString().Contains("%136 = load i32, ptr %39, align 4"))
            {
                Debugger.Break();
            }
            */
            var repl = ProcessBinaryLoad(loadInst);
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

            //Console.WriteLine($"Processing load: {loadInst}");

            var loadSize = LoadSizeOf(loadInst.TypeOf);
            Debug.Assert(loadSize <= 8);

            var loadBaseAndOffset = GetCanonicalBasePlusOffset(loadInst.GetOperand(0));
            var loadOffset = loadBaseAndOffset.Offset;


            // For each loaded byte, keep track of:
            //  (a) - the LLVM value that is being stored at that load offset
            //  (b) - the byte index of the LLVM value that needs to be fetched
            StoreOffsetMapping handledBytesToValue = new();

            var current = firstAccess;
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
                    Console.WriteLine("   --> Final store found! Bailing out.");
                    break;
                }
            }

            // If all bytes of the load are known:
            if (handledBytesToValue.Count == loadSize)
            {
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
                // Try to model this as an addition between a base pointer and a select between two constants.
                // Return null if we can't.
                var baseWithConstantSelect = KnownIndexStoreToLoadPropagation.GetAsBaseWithConstantSelect(loadInst);
                if (baseWithConstantSelect == null)
                    return null;

                // Position the builder before the original instruction.
                builder.PositionBefore(loadInst);

                // Calculate two separate memory pointer indices using the two constant indices and shared base pointer.
                var index1 = builder.BuildAdd(baseWithConstantSelect.BasePtr, baseWithConstantSelect.SelectOfTwoConstantIndices.GetOperand(1));
                var index2 = builder.BuildAdd(baseWithConstantSelect.BasePtr, baseWithConstantSelect.SelectOfTwoConstantIndices.GetOperand(2));

                // Compute two gep instructions.
                var ptrTy = function.GetFunctionCtx().GetPtrType();
                var gep1 = builder.BuildInBoundsGEP2(ptrTy, memPtr, new LLVMValueRef[] { index1 });
                var gep2 = builder.BuildInBoundsGEP2(ptrTy, memPtr, new LLVMValueRef[] { index2 });

                // Create the loads
                LLVMValueRef load1 = null;
                LLVMValueRef load2 = null;
                using (var updater = new MemorySSAUpdater(mssa))
                {
                    load1 = builder.BuildLoad2(loadInst.TypeOf, gep1);
                    load2 = builder.BuildLoad2(loadInst.TypeOf, gep2);

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
                }

                var solution1 = ProcessLoad(load1);
                if(solution1 == null)
                {
                    Console.WriteLine("Bailing out: First memory location could not be resolved to another load.");
                    return null;
                }

                // Try to solve the second load.
                var solution2 = ProcessLoad(load2);
                if (solution2 == null)
                {
                    Console.WriteLine("Bailing out: Second memory location could not be resolved to another load.");
                    return null;
                }

                // Ok, we were able to propagate both load destinations to other loads.
                var selectCond = baseWithConstantSelect.SelectOfTwoConstantIndices.GetOperand(0);
                var combined = builder.BuildSelect(selectCond, solution1, solution2);
                return combined;
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

        private LLVMValueRef CreateFullReplacementOfLoad(LLVMValueRef loadInst, StoreOffsetMapping storeOffsetMapping)
        {
            // Position the builder immediately before the load instruction.
            builder.PositionBefore(loadInst);

            // Sort the bytes by their index.
            var ordered = storeOffsetMapping.OrderBy(x => x.Key);

            var destTy = loadInst.TypeOf;
            LLVMValueRef last = LLVMValueRef.CreateConstInt(destTy, 0);
            var byteType = LLVMTypeRef.Int8;
            foreach (var handledByte in storeOffsetMapping.OrderBy(x => x.Key))
            {
                // Get the LLVMValueRef that this byte is coming form.
                var srcValue = handledByte.Value.byteSource;

                // Shift the isolated byte down to the very bottom byte position.
                var lshrBy = LLVMValueRef.CreateConstInt(srcValue.TypeOf, 8 * (handledByte.Value.byteIndex));
                var shifted = builder.BuildLShr(srcValue, lshrBy);

                // Isolate out only the last byte.
                var byteMask = LLVMValueRef.CreateConstInt(srcValue.TypeOf, 255);
                var isolated = builder.BuildAnd(shifted, byteMask);
                isolated = builder.BuildTrunc(isolated, byteType);

                // Upcast the value to the size of the load.
                isolated = builder.BuildZExt(isolated, loadInst.TypeOf);

                // Left shift the isolated byte back into it's correct position(relative to the load).
                var shlBy = LLVMValueRef.CreateConstInt(destTy, 8 * (ulong)(handledByte.Key));
                var orMask = builder.BuildShl(isolated, shlBy);

                // Bitwise OR the isolated byte back into the target.
                last = builder.BuildOr(last, orMask);
            }

            return last;
        }

        private LLVMValueRef ProcessBinaryLoad(LLVMValueRef loadInst)
        {
            // Type check
            if (loadInst.TypeOf.Kind != LLVMTypeKind.LLVMIntegerTypeKind)
                return null;

            // Get the GEP.
            var gep = loadInst.GetOperand(0);
            if (gep.InstructionOpcode != LLVMOpcode.LLVMGetElementPtr)
                return null;

            // If this is not a binary section access, do a last-ditch attempt with a load-of-select.
            if (!BinaryAccessMatcher.IsConstantWithinBinarySection(bin, gep.GetOperand(1)))
                return TryProcessAsLoadOfTwoPossibleAddresses(gep, loadInst);

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
            return constantInt;
        }

        private LLVMValueRef TryProcessAsLoadOfTwoPossibleAddresses(LLVMValueRef gep, LLVMValueRef loadInst)
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

            builder.PositionBefore(loadInst);

            // Construct loads for both constant sections.
            var ptrTy = function.GetFunctionCtx().GetPtrType();
            var gep1 = builder.BuildGEP2(ptrTy, gep.GetOperand(0), new LLVMValueRef[] { op1 });
            var gep2 = builder.BuildGEP2(ptrTy, gep.GetOperand(0), new LLVMValueRef[] { op2 });
            var load1 = builder.BuildLoad2(loadInst.TypeOf, gep1);
            var load2 = builder.BuildLoad2(loadInst.TypeOf, gep2);

            var select = builder.BuildSelect(selectPtr.GetOperand(0), load1, load2);
            return select;
        }
    }
}
