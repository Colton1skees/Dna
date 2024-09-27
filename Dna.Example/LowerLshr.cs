using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Example
{
    public static class LowerLshr
    {
        public static LLVMValueRef LowerLshrToLlvm(LLVMValueRef lshrInst, LLVMBuilderRef builder)
        {
            var intWidth = lshrInst.TypeOf.IntWidth;
            uint shiftCount = (uint)lshrInst.GetOperand(1).ConstIntZExt;
            var source = lshrInst.GetOperand(0);

            var bitSize = intWidth;
            var intTy = LLVMTypeRef.CreateInt(intWidth);
            LLVMValueRef b = LLVMValueRef.CreateConstInt(intTy, 0);
            LLVMValueRef mask1 = LLVMValueRef.CreateConstInt(intTy, 1);
            LLVMValueRef mask2 = LLVMValueRef.CreateConstInt(intTy, 1);
            mask2 = builder.BuildShl(LLVMValueRef.CreateConstInt(intTy, 1), LLVMValueRef.CreateConstInt(intTy, shiftCount));
            for (int i = 0; i < bitSize - shiftCount; i++)
            {
                var andMask = builder.BuildAnd(source, mask2);
                var cond = builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, andMask, LLVMValueRef.CreateConstInt(intTy, 0));
                b = builder.BuildSelect(cond, builder.BuildOr(b, mask2), b);
                mask1 = builder.BuildAdd(mask1, mask1);
                mask2 = builder.BuildAdd(mask2, mask2);

                //var cond = builder.BuildNot(builder.BuildAnd());
            }

            return b;
        }

    }
}
