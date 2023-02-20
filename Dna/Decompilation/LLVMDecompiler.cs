using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAssembly;

namespace Dna.Decompilation
{
    public class LLVMDecompiler
    {
        // TODO: Remove hardcoded path.
        private const string clangPath = @"C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\Llvm\x64\bin\clang.exe";

        public void Decompile(string wasmPath)
        {
            var module = Module.ReadFromBinary(wasmPath);
            Console.WriteLine(module);
        }

        public void RunClang(string arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = clangPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            Console.WriteLine(clangPath + " " + arguments);
            process.WaitForExit();
            if(process.ExitCode != 0 )
            {
                throw new Exception("command failed.");
            }
        }
    }
}
