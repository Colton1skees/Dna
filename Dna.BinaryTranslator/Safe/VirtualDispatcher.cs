using Dna.BinaryTranslator.Lifting;
using Dna.ControlFlow;
using Dna.ControlFlow.Extensions;
using Dna.Extensions;
using Dna.SEH;
using Iced.Intel;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X86Block = Dna.ControlFlow.BasicBlock<Iced.Intel.Instruction>;

namespace Dna.BinaryTranslator.Safe
{
    public record ScopeTableNodeDispatchInfo(ScopeTableNode Node, LiftedSehEntry LiftedSehEntry, HashSet<X86Block> OwnedTargets, HashSet<X86Block> ChildOwnedDispatcherTargets);

    public record DispatcherTarget(X86Block block, ScopeTableNode owner);

    public class VirtualDispatcher
    {
        private readonly LLVMValueRef function;

        private readonly ControlFlowGraph<Instruction> cfg;

        private readonly ScopeTableTree scopeTableTree;

        private readonly IReadOnlyList<LiftedSehEntry> liftedSehEntries;

        private readonly IReadOnlyDictionary<X86Block, LLVMBasicBlockRef> blockMapping;

        private readonly IReadOnlySet<X86Block> dispatcherTargets;

        private LLVMModuleRef Module => function.GlobalParent;

        private LLVMContextRef Ctx => Module.Context;

        public static LLVMValueRef CreateInFunction(LLVMValueRef function, ControlFlowGraph<Instruction> cfg, ScopeTableTree scopeTableTree, IReadOnlyList<LiftedSehEntry> liftedSehEntries,  IReadOnlyDictionary<X86Block, LLVMBasicBlockRef> blockMapping, IReadOnlySet<X86Block> dispatcherTargets)
            => new VirtualDispatcher(function, cfg, scopeTableTree, liftedSehEntries, blockMapping, dispatcherTargets).Apply();

        private VirtualDispatcher(LLVMValueRef function, ControlFlowGraph<Instruction> cfg, ScopeTableTree scopeTableTree, IReadOnlyList<LiftedSehEntry> liftedSehEntries, IReadOnlyDictionary<X86Block, LLVMBasicBlockRef> blockMapping, IReadOnlySet<X86Block> dispatcherTargets)
        {
            this.function = function;
            this.cfg = cfg;
            this.scopeTableTree = scopeTableTree;
            this.liftedSehEntries = liftedSehEntries;
            this.blockMapping = blockMapping;
            this.dispatcherTargets = dispatcherTargets;
        }

        private LLVMValueRef Apply()
        {
         //   File.WriteAllText("translatedFunction.ll", function.ToString());
       //     Debugger.Break();

            // Create a global dispatcher variable.
            var dispatcherKey = CreateGlobalKeyVariable();

            // Modify the function such that the entry point jumps to a 'virtual dispatcher'
            // which selects the actual virtualized block depending on a runtime key value.
            // This is necessary so that execution can resume within the same LLVM IR function
            // after a vmexit for a call.
            InsertVirtualDispatcher(dispatcherKey);

            // Lastly we modify all remill_call_intrinsic invocations such that the
            // last parameter contains an `inttoptr next_key`. This hardcoded key is used
            // later on to generate a VmReEntry to enter back into the VM after the function call.
            InsertVmReEntryKeyIntoCallIntrinsics();

            return dispatcherKey;
        }

        private LLVMValueRef CreateGlobalKeyVariable()
        {
            // Throw if a virtual dispatcher already exists. 
            // TODO: Allow this utility class to be applied to multiple different 
            // functions in the same module.
            var varName = "virtual_dispatch_key";
            if (Module.GetGlobals().Any(x => x.Name.Contains(varName)))
                throw new InvalidOperationException("Global dispatcher already exists.");

            // Create the type of the dispatcher variable.
            var type = Ctx.GetInt64Ty();

            // Create the dispatcher variable.
            var dispatcherVariable = Module.AddGlobal(type, varName);

            // Initialize and configure the variable.
            dispatcherVariable.Linkage = LLVMLinkage.LLVMExternalLinkage;
            return dispatcherVariable;
        
        }

        private void InsertVirtualDispatcher(LLVMValueRef globalDispatcherKey)
        {
            // Assert that no duplicate dispatch targets exist.
            AssertNoDuplicateDispatchTargets();

            // Assert that optimizations have not been applied yet(which would result in our artificial entry block being merged with a legitimate block.
            var entryBlock = function.EntryBasicBlock;
            if (entryBlock.Terminator.InstructionOpcode != LLVMOpcode.LLVMBr)
            {
                var errMsg = $"Basic block {entryBlock} does not end with an unconditional branch. This likely means that function optimizations have been applied, which should not have happened at this point in the virtualization process.";
                throw new InvalidOperationException(errMsg);
            }

            // Erase the current terminator to replace it.
            entryBlock.Terminator.InstructionEraseFromParent();
            EmitVirtualDispatcherSwitch(entryBlock, globalDispatcherKey);
        }

        private void AssertNoDuplicateDispatchTargets()
        {
            HashSet<ulong> seenAddresses = new();
            HashSet<string> seenNames = new();

            foreach(var target in dispatcherTargets)
            {
                // Assert that no blocks with duplicate addresses or names exist.
                if (seenAddresses.Contains(target.Address))
                    throw new InvalidOperationException($"Found duplicate blocks with address 0x{target.Address.ToString("X")}");
                if (seenNames.Contains(target.Name))
                    throw new InvalidOperationException($"Found duplicate blocks with name {target.Name}");

                seenAddresses.Add(target.Address);
                seenNames.Add(target.Name);
            }
        }

        /// <summary>
        /// Inserts a switch statement into the entry basic block, which jumps to a set of targets
        /// depending on a specified key(note: the key is the address of the basic block).
        /// We do this so that after a vm-entry, we can re-enter at the correct point.
        /// </summary>
        /// <param name="entryBlock"></param>
        /// <param name="globalDispatcherKey"></param>
        private void EmitVirtualDispatcherSwitch(LLVMBasicBlockRef entryBlock, LLVMValueRef globalDispatcherKey)
        {
            // Create an unreachable LLVM IR basic block to be used as the default case in our switch.
            var builder = LLVMBuilderRef.Create(Ctx);
            var unreachableBlock = CreateUnreachableBlock(builder);

            // Build a mapping of which dispatcher targets are inside of trys and which dispatcher targets are outside of trys.
            var targetsInsideOfTrys = new HashSet<X86Block>();
            var targetsOutsideTrys = new HashSet<X86Block>();
            foreach (var target in dispatcherTargets)
            {
                if(scopeTableTree.RootNodes.Any(x => x.ContainsAddress(target.Address)))
                    targetsInsideOfTrys.Add(target);
                else
                    targetsOutsideTrys.Add(target);
            }

            // For each scope table entry, collect a list of all child dispatcher targets, aswell as an unrolled set of childrens children.
            var clone = targetsInsideOfTrys.ToHashSet();
            var nodeDispatchInfoMapping = new Dictionary<ScopeTableNode, ScopeTableNodeDispatchInfo>();
            foreach (var root in scopeTableTree.RootNodes)
                CollectImmediateReentryOwners(clone, root, nodeDispatchInfoMapping);

            // Load the dispatcher key.
            builder.PositionAtEnd(entryBlock);
            var key = builder.BuildLoad2(Ctx.Int64Type, globalDispatcherKey, "dispatcher_key");

            // Build the set of cases for the root dispatcher.
            Dictionary<ulong, LLVMBasicBlockRef> caseMapping = new();
            foreach(var targetOutsideOfTry in targetsOutsideTrys)
                caseMapping.Add(targetOutsideOfTry.Address, blockMapping[targetOutsideOfTry]);

            // For each dispatch target in a root try node, add dispatch target that branches to the @try.begin preheader.
            // For each dispatch target that is inside of a root try node, add a dispatch target that goes to it's containing root node's @try.begin preheader.
            foreach(var rootTry in scopeTableTree.RootNodes)
            {
                // Collect all dispatcher targets inside of the try, including those inside of nested child TRYs.
                var dispatchInfo = nodeDispatchInfoMapping[rootTry];
                var cases = GetDispatchCasesForScopeTableEntry(dispatchInfo, nodeDispatchInfoMapping, true);
                foreach ((ulong caseKey, var block) in cases)
                    caseMapping.Add(caseKey, block);
            }

            // Insert a switch statement which jumps to the vm re-entry points based upon the key.
            // If the re-enter point is inside of a TRY statement then we need jump to the @try.begin()
            // macro.
            CreateSwitch(builder, key, caseMapping, unreachableBlock);

            // Insert virtual dispatchers in a breadth first order.
            for(int i = 0; i < scopeTableTree.DepthMapping.Length; i++)
            {
                var outermostScopes = scopeTableTree.DepthMapping[i];
                foreach(var outermost in outermostScopes)
                {
                    // Build a set of switch statement cases for the dispatcher.
                    var info = nodeDispatchInfoMapping[outermost];
                    var cases = GetDispatchCasesForScopeTableEntry(info, nodeDispatchInfoMapping, false);

                    // Take the "dispatcher preheader block" (an empty basic block that is the target of the @try.begin macro)
                    // and delete the only instruction inside of it(the direct branch).
                    var dispatcherPreheader = info.LiftedSehEntry.DispatcherPreheader;
                    dispatcherPreheader.GetInstructions().Single().InstructionEraseFromParent();

                    // Insert a switch statement / virtual dispatcher.
                    builder.PositionAtEnd(dispatcherPreheader);
                    CreateSwitch(builder, key, cases, unreachableBlock);
                }
            }

           // File.WriteAllText("translatedFunction.ll", function.ToString());
            //Debugger.Break();
        }

        private LLVMBasicBlockRef CreateUnreachableBlock(LLVMBuilderRef builder)
        {
            var unreachableBlock = function.AppendBasicBlock("unreachable_dispatch_target");
            builder.PositionAtEnd(unreachableBlock);
            builder.BuildUnreachable();
            return unreachableBlock;
        }

        /// <summary>
        /// For each dispatcher target that is inside of a try statement, keep track of it's 'immediate' owner, i.e. the innermost try it belongs to.
        /// </summary>
        /// <param name="unclaimedTargets"></param>
        /// <param name="node"></param>
        /// <param name="ownerMapping"></param>
        private void CollectImmediateReentryOwners(HashSet<X86Block> unclaimedTargets, ScopeTableNode node, Dictionary<ScopeTableNode, ScopeTableNodeDispatchInfo> ownerMapping)
        {
            // Visit all children.
            foreach (var child in node.Children)
                CollectImmediateReentryOwners(unclaimedTargets, child, ownerMapping);

            // Get all re-entry points that are owned by the current try catch scope. 
            // Note that since nested try catches are processed first, any claimed node here
            // is immediately owned by the current node.
            var dispatchInfo = new ScopeTableNodeDispatchInfo(node, liftedSehEntries.Single(x => x.ScopeTableNode == node), new(), new());
            ownerMapping[node] = dispatchInfo;
            var claimedTargets = unclaimedTargets.Where(x => node.ContainsAddress(x.Address)).ToList();
            foreach (var claimed in claimedTargets)
            {
                // Add this claimed target to the owned targets list.
                dispatchInfo.OwnedTargets.Add(claimed);

                // Remove the item from the unclaimed list.
                unclaimedTargets.Remove(claimed);
            }

            // Recursively hoist the child dispatcher targets up into 
            foreach (var child in node.Children)
            {
                var childInfo = ownerMapping[child];
                dispatchInfo.ChildOwnedDispatcherTargets.AddRange(childInfo.OwnedTargets);
                dispatchInfo.ChildOwnedDispatcherTargets.AddRange(childInfo.ChildOwnedDispatcherTargets);
            }
        }

        private Dictionary<ulong, LLVMBasicBlockRef> GetDispatchCasesForScopeTableEntry(ScopeTableNodeDispatchInfo dispatchInfo, Dictionary<ScopeTableNode, ScopeTableNodeDispatchInfo> ownerMapping, bool isOutside)
        {
            Dictionary<ulong, LLVMBasicBlockRef> caseMapping = new();

            if (isOutside)
            {
                // Collect all dispatcher targets inside of the try, including those inside of nested child TRYs.
                var allTargets = dispatchInfo.OwnedTargets.Union(dispatchInfo.ChildOwnedDispatcherTargets);

                // For each possible dispatcher target inside of the try, add a switch statement entry
                // tbhat goes to the @try.begin() invocation for the try statement.
                foreach (var target in allTargets)
                    caseMapping.Add(target.Address, dispatchInfo.LiftedSehEntry.PreheaderBlock);
            }

            // Otherwise we're inside the try.
            else
            {
                foreach (var inside in dispatchInfo.OwnedTargets)
                    caseMapping.Add(inside.Address, blockMapping[inside]);
                foreach (var outside in dispatchInfo.ChildOwnedDispatcherTargets)
                {
                    var nextOwner = dispatchInfo.Node.Children.Single(x => x.ContainsAddress(outside.Address));
                    caseMapping.Add(outside.Address, ownerMapping[nextOwner].LiftedSehEntry.PreheaderBlock);
                }
            }
            
            return caseMapping;
        }

        private LLVMValueRef CreateSwitch(LLVMBuilderRef builder, LLVMValueRef key, Dictionary<ulong, LLVMBasicBlockRef> dispatchCases, LLVMBasicBlockRef unreachableBlock)
        {
            // Build a switch, responsible for dispatching to the correct target.
            var swtch = builder.BuildSwitch(key, unreachableBlock, (uint)(dispatchCases.Count + 1));

            // Add a switch case to allow jumping to each possible jump target.
            foreach (var (vmKey, target) in dispatchCases)
            {
                // Commenting this out because I don't understand why I created this in the first place.
              //  // Build a BR to the lifted dispatcher target.
              //  var dispatcherBlock = function.AppendBasicBlock("dispatch_" + target.Name);
              //  builder.PositionAtEnd(dispatcherBlock);
               // builder.BuildBr(blockMapping[target]);

                // Add a case to our switch.
                // If the key is equal to the address of the target block, we BR to it.
                swtch.AddCase(LLVMValueRef.CreateConstInt(Ctx.Int64Type, vmKey), target);
            }

            return swtch;
        }

        private void InsertVmReEntryKeyIntoCallIntrinsics()
        {
            return;
            // For each basic block, check if contains an remill call intrinsic.
            // If it does contain a call intrinsic, then we must update the 3rd parameter of the call intrinsic
            // to contain our constant "vm re-entry key", which is used as a key to select the correct execution
            // point after re-entering the vm after the call execution.

            var targets = function.GetInstructions().Where(x => x.InstructionOpcode == LLVMOpcode.LLVMCall && x.GetOperands().Last().Name.Contains("__remill_function_call")).ToHashSet();


            foreach((X86Block x86Block, LLVMBasicBlockRef llvmBlock) in blockMapping)
            {
                // Skip if the lifted code does not contain an __remill_function_call.
                var callInst = llvmBlock.GetInstructions().SingleOrDefault(x => x.InstructionOpcode == LLVMOpcode.LLVMCall && x.GetOperands().Last().Name.Contains("__remill_function_call"), null);
                if (targets.Contains(callInst))
                    targets.Remove(callInst);
                if (callInst == null)
                    continue;

                // The dispatcher targets is a set of all basic blocks which are considered "vm entry or re-entry points".
                // If the target basic block after this call is not a valid dispatcher target, then something has gone horribly wrong,
                var dest = x86Block.GetOutgoingEdges().Single().TargetBlock;
                if (!dispatcherTargets.Contains(dest))
                    throw new InvalidOperationException($"Found remill_call intrinsic, but the resume point is not a valid dispatcher targets");

                // Replace the last operand of remill_function_call with an `inttoptr` of the address of re-entry block.
                // Later on we then use this parameter to build up a vm re-entry stub.
                var key = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, dest.Address);
                callInst.SetOperand(2, LLVMValueRef.CreateConstIntToPtr(key, Ctx.GetPtrType()));
            }

            Console.WriteLine("");
            Console.WriteLine(GraphFormatter.FormatGraph(cfg));


            Console.WriteLine("foobar.");
            Console.WriteLine("Left targets: ");
            foreach(var target in targets)
            {
                Console.WriteLine("    " + target);
            }

            function.GlobalParent.WriteToLlFile("translatedFunction.ll");
            Console.WriteLine("foobar");
        }
    }
}
