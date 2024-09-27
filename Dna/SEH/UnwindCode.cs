using AsmResolver.PE.File;
using Dna.Binary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.SEH
{
    /// <summary>
    ///     UnwindOp Codes for the unwind information
    ///     used to walk the stack in x64 applications.
    /// </summary>
    public enum UnwindOpType : byte
    {
        /// <summary>
        /// Push a nonvolatile integer register, decrementing RSP by 8. The
        /// operation info is the number of the register. Because of the constraints
        /// on epilogs, UWOP_PUSH_NONVOL unwind codes must appear first in the
        /// prolog and correspondingly, last in the unwind code array. This relative
        /// ordering applies to all other unwind codes except UWOP_PUSH_MACHFRAME.
        /// </summary>
        UwOpPushNonVol = 0,

        /// <summary>
        /// Allocate a large-sized area on the stack. There are two forms. If the
        /// operation info equals 0, then the size of the allocation divided by 8 is
        /// recorded in the next slot, allowing an allocation up to 512K - 8. If the
        /// operation info equals 1, then the unscaled size of the allocation is
        /// recorded in the next two slots in little-endian format, allowing
        /// allocations up to 4GB - 8.
        /// </summary>
        UwOpAllocLarge = 1,

        /// <summary>
        /// Allocate a small-sized area on the stack. The size of the allocation is
        /// the operation info field * 8 + 8, allowing allocations from 8 to 128
        /// bytes.
        /// </summary>
        UwOpAllocSmall = 2,

        /// <summary>
        /// Establish the frame pointer register by setting the register to some
        /// offset of the current RSP. The offset is equal to the Frame Register
        /// offset (scaled, field in the UNWIND_INFO * 16, allowing offsets from 0
        /// to 240. The use of an offset permits establishing a frame pointer that
        /// points to the middle of the fixed stack allocation, helping code density
        /// by allowing more accesses to use short instruction forms. The operation
        /// info field is reserved and shouldn't be used.
        /// </summary>
        UwOpSetFpReg = 3,

        /// <summary>
        /// Save a nonvolatile integer register on the stack using a MOV instead of
        /// a PUSH. This code is primarily used for shrink-wrapping, where a
        /// nonvolatile register is saved to the stack in a position that was
        /// previously allocated. The operation info is the number of the register.
        /// The scaled-by-8 stack offset is recorded in the next unwind operation
        /// code slot, as described in the note above.
        /// </summary>
        UwOpSaveNonVol = 4,

        /// <summary>
        /// Save a nonvolatile integer register on the stack with a long offset,
        /// using a MOV instead of a PUSH. This code is primarily used for
        /// shrink-wrapping, where a nonvolatile register is saved to the stack in a
        /// position that was previously allocated. The operation info is the number
        /// of the register. The unscaled stack offset is recorded in the next two
        /// unwind operation code slots, as described in the note above.
        /// </summary>
        UwOpSaveNonVolFar = 5,

        /// <summary>
        /// For version 1 of the UNWIND_INFO structure, this code was called
        /// UWOP_SAVE_XMM and occupied 2 records, it retained the lower 64 bits of
        /// the XMM register, but was later removed and is now skipped. In practice,
        /// this code has never been used.
        /// For version 2 of the UNWIND_INFO structure, this code is called
        /// UWOP_EPILOG, takes 2 entries, and describes the function epilogue.
        /// </summary>
        UwOpEpilog = 6,

        /// <summary>
        /// For version 1 of the UNWIND_INFO structure, this code was called
        /// UWOP_SAVE_XMM_FAR and occupied 3 records, it saved the lower 64 bits of
        /// the XMM register, but was later removed and is now skipped. In practice,
        /// this code has never been used.
        /// For version 2 of the UNWIND_INFO structure, this code is called
        /// UWOP_SPARE_CODE, takes 3 entries, and makes no sense.
        /// </summary>
        UwOpSpareCode = 7,

        /// <summary>
        /// Save all 128 bits of a nonvolatile XMM register on the stack. The
        /// operation info is the number of the register. The scaled-by-16 stack
        /// offset is recorded in the next slot.
        /// </summary>
        UwOpSaveXmm128 = 8,

        /// <summary>
        /// Save all 128 bits of a nonvolatile XMM register on the stack with a long
        /// offset. The operation info is the number of the register. The unscaled
        /// stack offset is recorded in the next two slots.
        /// </summary>
        UwOpSaveXmm128Far = 9,

        /// <summary>
        /// Push a machine frame. This unwind code is used to record the effect of a
        /// hardware interrupt or exception.
        /// </summary>
        UwOpPushMachFrame = 10,

        /// <summary>
        /// UWOP_SET_FPREG_LARGE is a CLR Unix-only extension to the Windows AMD64
        /// unwind codes. It is not part of the standard Windows AMD64 unwind codes
        /// specification. UWOP_SET_FPREG allows for a maximum of a 240 byte offset
        /// between RSP and the frame pointer, when the frame pointer is
        /// established. UWOP_SET_FPREG_LARGE has a 32-bit range scaled by 16. When
        /// UWOP_SET_FPREG_LARGE is used, UNWIND_INFO.FrameRegister must be set to
        /// the frame pointer register, and UNWIND_INFO.FrameOffset must be set to
        /// 15 (its maximum value). UWOP_SET_FPREG_LARGE is followed by two
        /// UNWIND_CODEs that are combined to form a 32-bit offset (the same as
        /// UWOP_SAVE_NONVOL_FAR). This offset is then scaled by 16. The result must
        /// be less than 2^32 (that is, the top 4 bits of the unscaled 32-bit number
        /// must be zero). This result is used as the frame pointer register offset
        /// from RSP at the time the frame pointer is established. Either
        /// UWOP_SET_FPREG or UWOP_SET_FPREG_LARGE can be used, but not both.
        /// </summary>
        UwOpSetFpRegLarge = 11,
    }

    public abstract record UnwindCode(byte CodeOffset, byte OpInfo);
    public record UwOpPushNonVol(byte CodeOffset, byte OpInfo, Iced.Intel.Register Register) : UnwindCode(CodeOffset, OpInfo);
    public record UwOpAllocLarge(byte CodeOffset, byte OpInfo, int Size) : UnwindCode(CodeOffset, OpInfo);
    public record UwOpAllocSmall(byte CodeOffset, byte OpInfo, int Size) : UnwindCode(CodeOffset, OpInfo);
    public record UwOpSetFpReg(byte CodeOffset, byte OpInfo, Iced.Intel.Register Register) : UnwindCode(CodeOffset, OpInfo);
    public record UwOpSaveNonVol(byte CodeOffset, byte OpInfo, Iced.Intel.Register Register, int FrameOffset) : UnwindCode(CodeOffset, OpInfo);
    public record UwOpSaveNonVolFar(byte CodeOffset, byte OpInfo, Iced.Intel.Register Register, int FrameOffset) : UnwindCode(CodeOffset, OpInfo);
    public record UwOpEpilog(byte CodeOffset, byte OpInfo) : UnwindCode(CodeOffset, OpInfo);
    public record UwOpSpareCode(byte CodeOffset, byte OpInfo) : UnwindCode(CodeOffset, OpInfo);
    public record UwOpSaveXmm128(byte CodeOffset, byte OpInfo, Iced.Intel.Register Register, int FrameOffset) : UnwindCode(CodeOffset, OpInfo);
    public record UwOpSaveXmm128Far(byte CodeOffset, byte OpInfo, Iced.Intel.Register Register, int FrameOffset) : UnwindCode(CodeOffset, OpInfo);
    public record UwOpPushMachFrame(byte CodeOffset, byte OpInfo) : UnwindCode(CodeOffset, OpInfo);
    public record UwOpSetFpRegFar(byte CodeOffset, byte OpInfo, Iced.Intel.Register Register) : UnwindCode(CodeOffset, OpInfo);
}
