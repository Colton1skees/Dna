using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Relocation
{
    public static class InstructionEncoder
    {
        public static byte[] EncodeInstruction(Instruction instruction, ulong sourceRIP)
        {
            return EncodeInstructions(new List<Instruction>() { instruction }, sourceRIP, out ulong endRIP);
        }

        public static byte[] EncodeInstructions(IList<Instruction> instructions, ulong sourceRIP, out ulong endRIP)
        {
            // Allocate a new encoder.
            var stream = new MemoryStream();
            StreamCodeWriter compiledWriter = new StreamCodeWriter(stream);
            var encoder = Iced.Intel.Encoder.Create(64, compiledWriter);
            ulong rip = 0;

            // Relocate the instructions. TODO: Refactor this to remove the redundant decoding.
            var relocatedInstructions = RelocateInstructions(instructions, sourceRIP);
            foreach (var insn in relocatedInstructions)
            {
                var result = encoder.Encode(insn, rip + sourceRIP);
                var codeReader = new ByteArrayCodeReader(stream.GetBuffer(), (int)rip, (int)result);
                var decoder = Iced.Intel.Decoder.Create(64, codeReader);
                decoder.IP = sourceRIP + rip;
                rip += (ulong)result;
            }

            endRIP = sourceRIP + rip;
            return stream.GetBuffer().Take((int)rip).ToArray();
        }

        public static IList<Instruction> RelocateInstructions(IList<Instruction> instructions, ulong rip)
        {
            // Attempt to relocate the instructions to the target rip.
            var codeWriter = new CodeWriterImpl();
            var block = new InstructionBlock(codeWriter, instructions, rip);
            bool success = BlockEncoder.TryEncode(64, block, out var errorMsg, out BlockEncoderResult result);
            if(!success)
                throw new Exception(errorMsg);

            // Initialize a decoder.
            var bytes = codeWriter.ToArray();
            var codeReader = new ByteArrayCodeReader(bytes);
            var decoder = Iced.Intel.Decoder.Create(64, codeReader);
            decoder.IP = rip;

            // Decode the newly relocated instructions.
            List<Instruction> output = new List<Instruction>();
            int decodedLength = 0;
            while(decodedLength != bytes.Length)
            {
                var instruction = decoder.Decode();
                decodedLength += instruction.Length;
                output.Add(instruction);
            }

            return output;
        }
    }

    sealed class CodeWriterImpl : CodeWriter
    {
        readonly List<byte> AllBytes = new List<byte>();

        public byte[] ToArray() => AllBytes.ToArray();

        public override void WriteByte(byte value) => AllBytes.Add(value);
    }
}
