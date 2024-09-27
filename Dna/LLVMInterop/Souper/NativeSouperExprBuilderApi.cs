using Dna.LLVMInterop.API;
using Dna.LLVMInterop.Souper.Inst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper
{
    public static class NativeSouperExprBuilderApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueExprBuilder* ExprBuilderConstructorKlee(SouperOpaqueInstContext* ctx);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern SouperOpaqueInst* ExprBuilderGetBlockPCs(SouperOpaqueExprBuilder* exprBuilder, SouperOpaqueInst* root);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* SouperBuildQuery(SouperOpaqueInstContext* ctx, OpaqueManagedVector<SouperOpaqueBlockPCMapping>* bpcs, OpaqueManagedVector<SouperOpaqueInstMapping>* pcs,
            SouperOpaqueInstMapping* mapping, OpaqueManagedVector<SouperOpaqueInst>* modelVars, SouperOpaqueInst* precondition, bool negate, bool dropUB);
    }
}
