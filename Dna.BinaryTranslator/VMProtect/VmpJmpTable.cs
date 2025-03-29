using Dna.ControlFlow.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.JmpTables
{
    public class VmpJmpTable
    {
        /// <summary>
        /// The address of the indirect jump instruction.
        /// </summary>
        public ulong JmpFromAddr { get; }

        /// <summary>
        /// The list of the possible outgoing addresses of the jump table.
        /// </summary>
        public HashSet<ulong> KnownOutgoingAddresses = new();

        /// <summary>
        /// The list of addresses of basic blocks that are predecessors of the jump table.
        /// </summary>
        public HashSet<ulong> KnownPredecessorAddresses = new();

        /// <summary>
        /// Gets whether the jump table is complete. A jump table is considered complete
        /// if we have solved for the set of outgoing addresses and no new cfg changes have occurred
        /// that may result in a widening of the set of jump table values.
        /// 
        /// To rephrase what 'incomplete' means:
        ///     A jump table is considered incomplete if the current set of outgoing edges is not guaranteed to include all possible outgoing targets.
        ///     The only time this happens is when a new potential back edge to a jump table is discovered. 
        ///     
        ///     An example case of when this happens can be found at `sub_140098660` in Notepad++.exe(https://drive.google.com/file/d/17pHm7BCgtw8DmJ0MrczYOuuy_mUOqt8n/view?usp=sharing).
        ///     After applying recursive descent to the function entrypoint, we are stopped by a jump table. So the iterative cfg translator will lift the partial control flow graph to LLVM IR,
        ///     optimize it, and then solve for the set of bounds using z3. However, this jump table has 5 entries, but applying the jump table solver only finds 1 entry.
        ///     This is because LLVM's built in optimizations were able to prove that in the current partial control flow graph, the default case of the jump table is always taken.
        ///     This results in concretization of the index pointer, yielding only 1 value after solving. But after adding the edge at jmpTable[index] and applying recursive descent again,
        ///     a new back edge to the jump table is discovered - potentially changing the set of outgoing edges & requiring that the control flow graph is reproved. 
        /// </summary>
        public bool IsComplete { get; set; }

        public VmpJmpTable(ulong jmpFromAddress, IReadOnlyList<ulong> outgoingAddresses, IReadOnlyList<ulong> knownPredecessors, bool isComplete = false)
        {
            JmpFromAddr = jmpFromAddress;
            IsComplete = isComplete;
            KnownOutgoingAddresses.AddRange(outgoingAddresses);
            KnownPredecessorAddresses.AddRange(knownPredecessors == null ? new List<ulong>() : knownPredecessors);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            var strComplete = IsComplete ? "Complete" : "Incomplete";
            sb.AppendLine($"{strComplete} Jump table 0x{JmpFromAddr.ToString("X")} with {KnownOutgoingAddresses.Count} unique entries:");
            foreach (var jmpAddress in KnownOutgoingAddresses)
            {
                sb.AppendLine($"    0x{jmpAddress.ToString("X")}");
            }
            return sb.ToString();
        }
    }
}
