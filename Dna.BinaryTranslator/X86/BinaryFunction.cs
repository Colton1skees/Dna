using Dna.BinaryTranslator.JmpTables;
using Dna.ControlFlow;
using Dna.SEH;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.X86
{
    public class BinaryFunction
    {
        /// <summary>
        /// The control flow graph of the binary function, alongside of a mapping of 
        /// assembly instructions to their encodings.
        /// </summary>
        public EncodedCfg<Instruction> EncodedCfg { get; }

        /// <summary>
        /// The control flow graph.
        /// </summary>
        public ControlFlowGraph<Instruction> Cfg => EncodedCfg.Cfg;

        /// <summary>
        /// A mapping of <rip, JmpTable> for solved jump tables within the control flow graph.
        /// Note that unsolved jump tables may exist - but not within this list.
        /// </summary>
        public IReadOnlyDictionary<ulong, JmpTable> JmpTables { get; }

        /// <summary>
        /// The SEH scope table.
        /// </summary>
        public ScopeTableTree ScopeTableTree { get; }

        public BinaryFunction(EncodedCfg<Instruction> encodedCfg, ScopeTableTree scopeTableTree, IReadOnlyList<JmpTable> jmpTables) : this(encodedCfg, scopeTableTree, jmpTables.ToDictionary(x => x.JmpFromAddr, x => x))
        {
        }

        public BinaryFunction(EncodedCfg<Instruction> encodedCfg, ScopeTableTree scopeTableTree, IReadOnlyDictionary<ulong, JmpTable> jmpTables)
        {
            EncodedCfg = encodedCfg;
            ScopeTableTree = scopeTableTree;
            JmpTables = jmpTables;
        }

        public byte[] GetInstructionEncodingAt(ulong address) => EncodedCfg.GetInstructionEncodingAt(address);
    }
}
