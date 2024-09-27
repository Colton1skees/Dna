using Dna.LLVMInterop.API.Remill.Arch;
using Dna.LLVMInterop.API.Remill.Manual;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.Remill.BC
{
    public class RemillInstructionLifterIntf : RemillOperandLifter
    {
        public RemillInstructionLifterIntf(nint handle) : base(handle)
        {

        }

        public unsafe RemillLiftStatus LiftIntoBlock(RemillInstruction inst, LLVMBasicBlockRef block, LLVMValueRef statePtr, bool isDelayed = false)
            => NativeRemillLifterApi.InstructionLifterIntf_LiftIntoBlockWithStatePtr(this, inst, block, statePtr, isDelayed);

        public unsafe RemillLiftStatus LiftIntoBlock(RemillInstruction inst, LLVMBasicBlockRef block, bool isDelayed = false)
            => NativeRemillLifterApi.InstructionLifterIntf_LiftIntoBlock(this, inst, block, isDelayed);

        public unsafe static implicit operator RemillOpaqueInstructionLifterIntf*(RemillInstructionLifterIntf reg) => (RemillOpaqueInstructionLifterIntf*)reg.Handle;

        public unsafe static implicit operator RemillInstructionLifterIntf(RemillOpaqueInstructionLifterIntf* reg) => new RemillInstructionLifterIntf((nint)reg);
    }
}
