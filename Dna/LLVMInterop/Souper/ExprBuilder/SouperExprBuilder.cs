using Dna.LLVMInterop.API;
using Dna.LLVMInterop.Souper.Inst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.Souper.ExprBuilder
{
    public class SouperExprBuilder
    {
        private readonly nint handle;

        public SouperExprBuilder(nint handle)
        {
            this.handle = handle;
        }

        public unsafe SouperExprBuilder(SouperInstContext instCtx)
        {
            handle = (nint)NativeSouperExprBuilderApi.ExprBuilderConstructorKlee(instCtx);
        }

        public unsafe SouperInst GetBlockPcs(SouperInst inst)
            => NativeSouperExprBuilderApi.ExprBuilderGetBlockPCs(this, inst);

        public unsafe string BuildQuery(SouperInstContext ctx, IReadOnlyList<SouperBlockPCMapping> bpcs, IReadOnlyList<SouperInstMapping> pcs,
            SouperInstMapping mapping, IReadOnlyList<SouperInst> modelVars, SouperInst precondition, bool negate = false, bool dropUB = false)
        {
            var unmanagedBpcs = ManagedVector<nint>.From(bpcs.Select(x => x.handle).ToArray(), x => x);
            var unmanagedPcs = ManagedVector<nint>.From(pcs.Select(x => x.handle).ToArray(), x => x);
            var unmanagedModelVars = ManagedVector<nint>.From(modelVars.Select(x => x.handle).ToArray(), x => x);

            var bpcsPtr = (OpaqueManagedVector<SouperOpaqueBlockPCMapping>*)unmanagedBpcs.Handle;
            var pcsPtr = (OpaqueManagedVector<SouperOpaqueInstMapping>*)unmanagedPcs.Handle;
            var modelVarsPtr = (OpaqueManagedVector<SouperOpaqueInst>*)unmanagedModelVars.Handle;

            var query = NativeSouperExprBuilderApi.SouperBuildQuery(ctx, bpcsPtr, pcsPtr, mapping, modelVarsPtr, precondition, negate, dropUB);
            return StringMarshaler.AcquireString(query);
        }

        public unsafe static implicit operator SouperOpaqueExprBuilder*(SouperExprBuilder block) => (SouperOpaqueExprBuilder*)block.handle;

        public unsafe static implicit operator SouperExprBuilder(SouperOpaqueExprBuilder* block) => new SouperExprBuilder((nint)block);
    }
}
