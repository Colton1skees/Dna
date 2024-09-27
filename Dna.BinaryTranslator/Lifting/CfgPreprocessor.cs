using Dna.BinaryTranslator.Safe;
using Dna.BinaryTranslator.X86;
using Dna.ControlFlow;
using Dna.Reconstruction;
using Dna.SEH;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Lifting
{
    /// <summary>
    /// Class for preprocessing a native control flow graph into a 'EncodedCfg` that's liftable directly to LLVM IR.
    /// </summary>
    public static class CfgPreprocessor
    {
        /// <summary>
        /// Take a CFG and apply a set of transforms that makes it directly liftable to LLVM IR.
        /// </summary>
        public static ControlFlowGraph<Instruction> ProcessCfg(ControlFlowGraph<Instruction> cfg, ScopeTableTree scTree)
        {
            // When Dna encounters a fallthrough edge, the fallthrough target is duplicated into it's predecessor.
            // As a first preprocessing step we undo this. This makes lifting and compiler optimization much faster on pathological cases,
            // because there is less work to do. Also note that it enforces
            // the property that a unique x86 instruction at address X will only appear exactly once in any control flow graph.
            cfg = FallthroughDeduplicator.DeduplicateFallthroughEdges(cfg, Enumerable.Empty<ulong>()).newCfg;

            // Next we enforce the property that there is a unique basic block starting at:
            //  - The beginning of each TRY guarded region
            //  - The end address of any TRY guarded region.
            //  - The handler address of any scope table entry.
            var sehPoints = scTree.ScopeTable.Entries.SelectMany(x => new List<ulong>() { x.BeginAddr, x.EndAddr, x.HandlerAddr }).ToList().ToHashSet();
            X86CfgSplitter.SplitBlocksAtSeh(cfg, sehPoints);

            // Build a mapping of <block address, block>.
            var addrToBlockMapping = cfg.GetBlocks().ToDictionary(x => x.Address, x => x);

            // Get a mapping of <scope table entry, try begin basic block>.
            var tryBeginBlocks = scTree.ScopeTable.Entries.Select(x => (x, addrToBlockMapping[x.BeginAddr]));

            // For each guarded try region, get all exiting edges out of the try. Note that this also includes edges which branch out of the current try and into a parent try.
            var exitingEdges = tryBeginBlocks.SelectMany(x => SehRegionAnalysis.GetExitingEdgesFromRegion(x.Item2, x.x.BeginAddr, x.x.EndAddr));

            // Enforce the property that the target of any exiting edge(including fallthrough edges or fallthrough to a parent TRY) out of a scope table entry
            // will have it's own basic block.  
            X86CfgSplitter.SplitBlocksAtSeh(cfg, exitingEdges.Select(x => x.TargetBlock.Address).ToHashSet());

            // Enforce the property that any instruction following a 'CALL' instruction marks the start of a new basic block.
            X86CfgSplitter.SplitBlocksAtCalls(cfg);

            return cfg;
        }
    }
}
