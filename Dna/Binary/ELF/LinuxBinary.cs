using AsmResolver.PE.File;
using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using ELFSharp.ELF.Segments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Binary.Windows
{
    public class LinuxBinary : IBinary
    {
        public int Bitness { get; }

        public byte[] Bytes { get; set; }

        public ulong BaseAddress { get; }

        public IELF ELFFile { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsBinary"/> class.
        /// </summary>
        /// <param name="bitness">The bitness of the binary(i.e 32 or 64).</param>
        /// <param name="binaryBytes">The raw bytes of the PE file.</param>
        /// <param name="baseAddress"> 
        ///     The optional base address of the binary.
        ///     If no address is provided, then base address specified in the optional header is used.
        /// </param>
        public LinuxBinary(int bitness, byte[] binaryBytes, ulong baseAddress)
        {
            Bitness = bitness;
            Bytes = binaryBytes;
            ELFFile = ELFReader.Load(new MemoryStream(binaryBytes), true);
            BaseAddress = baseAddress;
        }

        /// <inheritdoc cref="IBinary.ReadBytes(ulong, int)"/>
        public byte[] ReadBytes(ulong address, int count = 15)
        {
            // Compute a zero based offset for the address.
            var offset = address - BaseAddress;

            // Compute the segment containing the address.
            // TODO: Properly handle data split across multiple segments.
            var section = ELFFile.Sections
                .Where(x => x is Section<ulong>)
                .Cast<Section<ulong>>()
                .Single(x => x.Offset <= offset && (x.Offset + x.Size) >= (offset + (ulong)count));

            // Allocate a buffer to store the results in.
            byte[] buffer = new byte[count];

            // Read the raw data from the binary.
            var segmentOffset = offset - section.Offset;
            Array.Copy(section.GetContents(), (int)segmentOffset, buffer, 0, count);
            return buffer;
        }

        /// <inheritdoc cref="IBinary.WriteBytes(ulong, byte[])"/>
        public void WriteBytes(ulong address, byte[] bytes)
        {
            throw new NotImplementedException();
        }
    }
}
