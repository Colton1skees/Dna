using Dna.SEH;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.BinaryTranslator.Lifting
{
    /// <summary>
    /// Class for representing a lifted ScopeTable entry.
    /// </summary>
    public class LiftedSehEntry
    {
        /// <summary>
        /// The scope table tree entry.
        /// </summary>
        public ScopeTableNode ScopeTableNode { get; }

        /// <summary>
        /// The preheader block marks the begin of a TRY statement. It invokes the llvm try.begin macro to signal the start of the try.
        /// Also note that the preheader block jumps to our artificial dispatcher preheader, which then jumps to the correct try target.
        /// </summary>
        public LLVMBasicBlockRef PreheaderBlock { get; }

        /// <summary>
        /// The dispatcher preheader is an artificial block inserted between the preheader block and the TRY implementation.
        /// llvm.try.begin() jumps to this basic block, and then this basic block immediately branches to the underlying TRY implementation block.
        /// We insert this artificial basic block so that our virtual dispatcher class can easily use this basic block to insert a switch statement
        /// as a re-entry dispatcher.
        /// </summary>
        public LLVMBasicBlockRef DispatcherPreheader { get; }

        /// <summary>
        /// The landing pad block. Alternatively it can be called the catch dispatch block.
        /// This is the block which executes LLVM's `catchswitch` instruction in the event of an exception.
        /// </summary>
        public LLVMBasicBlockRef LandingPadBlock { get; }

        /// <summary>
        /// Class for representing a lifted filter function. 
        /// </summary>
        public LiftedFilterFunction LiftedFilterFunction { get; }

        public LiftedSehEntry(ScopeTableNode node, LLVMBasicBlockRef preheaderBlock, LLVMBasicBlockRef dispatcherPreheader, LLVMBasicBlockRef landingPadBlock, LiftedFilterFunction liftedFilterFunction)
        {
            ScopeTableNode = node;
            PreheaderBlock = preheaderBlock;
            DispatcherPreheader = dispatcherPreheader;
            LandingPadBlock = landingPadBlock;
            LiftedFilterFunction = liftedFilterFunction;
        }
    }

    /// <summary>
    /// Class representing a placeholder lifted filter function.
    /// </summary>
    /// <param name="Address">The native address of the filter function.</param>
    /// <param name="LlvmFunction">The LLVM function implementing the filter. Note that this doesn't actually contain the lifted function - instead it invokes the native function directly by casting a pointer.</param>
    /// <param name="RspGlobal">Global variable that contains the spilled state structure's RSP value. This is then fetched in the filter function to spoof the stack pointer that is passed to the native filter function.</param>
    /// <param name="ImagebaseGlobal">Global variable containing the image base.</param>
    public record LiftedFilterFunction(ulong Address, LLVMValueRef LlvmFunction, LLVMValueRef RspGlobal, LLVMValueRef ImagebaseGlobal);
}
