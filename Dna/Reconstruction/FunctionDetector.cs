using Dna.Binary;
using Dna.Binary.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Reconstruction
{
    /// <summary>
    /// Bounds of a function from within the binary. Note that `EndAddr` is inclusive, i.e. it points to the first byte after the function's last byte. 
    /// </summary>
    public record FunctionBounds(ulong StartAddr, ulong EndAddr);

    /// <summary>
    /// Class for parsing all available function bounds from the .pdata section of the binary.
    /// </summary>
    public class FunctionDetector
    {
        private readonly IBinary bin;

        public static IReadOnlyList<FunctionBounds> Run(IBinary binary)
            => new FunctionDetector(binary).GetFunctions();

        private FunctionDetector(IBinary binary)
        {
            this.bin = binary;
        }

        // Parse all function bounds out the SEH tables.
        private IReadOnlyList<FunctionBounds> GetFunctions()
        {
            if (bin is not WindowsBinary windowsBinary)
                throw new InvalidOperationException($"Analysis only supported for windows binaries!");

            var functions = new List<FunctionBounds>();
            var binary = windowsBinary;
            var section = binary.PEFile.Sections.Single(x => x.Name.Contains("pdata"));
            var bytes = binary.Bytes.Skip((int)section.Offset).Take((int)section.GetVirtualSize()).ToArray();

            byte[] offsetBytes = new byte[4];
            for (int i = 0; i < bytes.Length - 1; i += 12)
            {
                Array.Copy(bytes, i, offsetBytes, 0, 4);
                var startOffset = (ulong)BitConverter.ToUInt32(offsetBytes, 0);
                var funcAddress = binary.BaseAddress + startOffset;
                if (funcAddress == binary.BaseAddress)
                {
                    continue;
                }


                Array.Copy(bytes, i + 4, offsetBytes, 0, 4);
                var endOffset = (ulong)BitConverter.ToUInt32(offsetBytes, 0);
                var endAddr = binary.BaseAddress + endOffset;
                functions.Add(new(funcAddress, endAddr));
            }

            return functions;
        }
    }
}
