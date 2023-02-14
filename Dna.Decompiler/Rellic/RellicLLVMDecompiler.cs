using Dna.Decompiler.Rellic;
using Google.Protobuf;
using Grpc.Net.Client;
using LLVMSharp;
using static RellicDecompilation;

namespace Dna.Decompilation;

/// <summary>
/// Class for decompiling LLVM modules to pseudo C.
/// </summary>
internal class RellicLLVMDecompiler
{
    private readonly GrpcChannel channel = GrpcChannel.ForAddress(new Uri("http://localhost:50051"));

    private readonly RellicDecompilationClient client;

    public RellicLLVMDecompiler()
    {
        client = new RellicDecompilationClient(channel);
    }

    /// <summary>
    /// Decompiles the provided LLVM module to go-to free pseudo C via Rellic.
    /// </summary>
    /// <param name="module"></param>
    /// <returns></returns>
    public string Decompile(LLVMModuleRef module)
    {
        // Set the LLVM target to x86_64. 
        LLVM.SetTarget(module, "x86_64");

        // Serialize the LLVM module to bitcode.
        var bitcode = LlvmUtilities.SerializeModuleToBC(module);

        var reply = client.Decompile(new DecompileCommand()
        {
            LlvmModuleText = ByteString.CopyFrom(bitcode)
        });

        return reply.DecompiledText;
    }
}