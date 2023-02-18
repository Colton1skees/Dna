using Dna.Binary;
using Dna.Binary.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Emulation
{
    /// <summary>
    /// Class for mapping windows binaries into an emulator's memory space.
    /// </summary>
    public static class PEMapper
    {
        public static void MapBinary(ICpuEmulator state, WindowsBinary binary)
        {
            state.MapMemory(binary.BaseAddress, 0x1000 * 100);
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
