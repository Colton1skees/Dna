using Dna.Binary;
using Dna.Binary.Windows;
using ELFSharp.ELF.Sections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Emulation
{
    /// <summary>
    /// Class for mapping binaries into an emulator's memory space.
    /// </summary>
    public static class BinaryMapper
    {
        public static void MapELFFile(ICpuEmulator state, LinuxBinary binary)
        {
            // TODO: Actually map the correct amount of memory.
            state.MapMemory(binary.BaseAddress, 0x1000 * 1000);

            // Get all mappable sections.
            var elfFile = binary.ELFFile;
            var sections = elfFile.Sections
                .Where(x => x is Section<ulong>)
                .Cast<Section<ulong>>();

            foreach(var section in sections)
            {
                // Compute the address to map the section at.
                var address = section.Offset + binary.BaseAddress;
            }
        }

        public static void MapPEFile(ICpuEmulator state, WindowsBinary binary)
        {
            state.MapMemory(binary.BaseAddress, 0x1000 * 1000);
            var peFile = binary.PEFile;
            foreach(var section in peFile.Sections)
            {
                // Get the section rva.
                var rva = section.Rva;

                // Compute the file offset of the section.
                var fileOffset = (int)peFile.RvaToFileOffset(rva);

                // Get the section bytes.
                var sectionBytes = new byte[section.GetPhysicalSize()];
                Array.Copy(binary.Bytes, fileOffset, sectionBytes, 0, sectionBytes.Length);

                state.WriteMemory(binary.BaseAddress + rva, sectionBytes);
            }
        }
    }
}
