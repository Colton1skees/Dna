using Dna.LLVMInterop.API.RegionAnalysis.Native;
using Dna.LLVMInterop.API.RegionAnalysis.Wrapper;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.LLVMBindings
{
    public unsafe abstract class Pass
    {
        public readonly nint Handle;

        public string Name => GetName();

        public PassKind Kind => NativePassApi.GetPassKind(this);

        public Pass(nint handle)
        {
            this.Handle = handle;
        }

        private unsafe string GetName()
        {
            var pStr = NativePassApi.GetPassName(this);
            if (pStr == null)
                return String.Empty;

            var result = SpanExtensions.AsString(pStr);
            LLVM.DisposeMessage(pStr);
            return result;
        }

        public static unsafe Pass FromPtr(LLVMOpaquePass* pass)
        {
            var kind = NativePassApi.GetPassKind(pass);
            return kind switch
            {
                PassKind.Function => new FunctionPass((nint)pass),
                PassKind.Module => new ModulePass((nint)pass),
                _ => throw new InvalidOperationException($"{kind} passes are not supported.")
            };
        }

        public unsafe static implicit operator LLVMOpaquePass*(Pass pass)
        {
            return (LLVMOpaquePass*)pass.Handle;
        }

        public unsafe static implicit operator Pass(LLVMOpaquePass* pass)
        {
            return FromPtr(pass);
        }
    }
}
