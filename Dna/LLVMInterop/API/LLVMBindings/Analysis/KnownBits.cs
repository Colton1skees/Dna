using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings.Analysis
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct NativeKnownBits
    {
        [FieldOffset(0)] ulong Zero;
        [FieldOffset(8)] ulong One;

        public ulong GetKnownMask()
        {
            return Zero | One;
        }

        public ulong GetUnknownMask()
        {
            return ~(Zero | One);
        }

        public int GetKnownBitCount()
        {
            return BitOperations.PopCount(GetKnownMask());
        }

        public int GetUnknownBitCount()
        {
            return BitOperations.PopCount(GetUnknownMask());
        }

        public ulong? SingleValue()
        {
            if ((Zero | One) == unchecked((ulong)-1))
                return One;

            return null;
        }

        public bool CanBeValue(ulong val)
        {
            var unknown = GetUnknownMask();
            return (val | unknown) == (One | unknown);
        }

        // WARNING: check GetUnknownCount() before calling this!
        public ulong[] AllPossibleValues()
        {
            Debug.Assert(GetUnknownBitCount() != 64);

            var unknown = 1UL << GetUnknownBitCount();
            var unkMask = GetUnknownMask();

            var arr = new ulong[unknown];
            for (ulong m = 0; m < unknown; ++m)
            {
                // PDEEEEEEEEEEP
                arr[m] = Bmi2.X64.ParallelBitDeposit(m, unkMask) | One;
            }

            return arr;
        }

        public static NativeKnownBits Get(LLVMValueRef inst, LLVMModuleRef module)
        {
            unsafe
            {
                var targetData = LLVM.GetModuleDataLayout(module);

                NativeKnownBits kb = new();
                GetKnownBits(inst.Handle, targetData, &kb);
                return kb;
            }
        }

        [DllImport("Dna.LLVMInterop", EntryPoint = "KnownBits_Get")]
        public unsafe static extern LLVMOpaqueTargetData* GetKnownBits(nint instruction, LLVMOpaqueTargetData* targetData, NativeKnownBits* outVal);
    }
}