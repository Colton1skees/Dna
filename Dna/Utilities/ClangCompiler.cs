using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Utilities
{
    public static class ClangCompiler
    {
        // TODO: Remove hardcoded path.
        private const string clangPath = @"C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\Llvm\x64\bin\clang.exe";

        public static string Compile(string llPath, bool overwrite = false)
        {
            // If the file already exists, we don't want to overwrite it.
            var dir = Path.GetDirectoryName(llPath);
            // Compile the .ll file to assembly with vectorization disabled.
            var fileName = Path.GetFileName(llPath);
            var asmPath = Path.Combine(dir, Path.ChangeExtension(fileName, ".asm"));
            RunClang(@$"""{llPath}"" -S -o ""{asmPath}"" -fno-vectorize -fno-slp-vectorize -O3  -mno-sse -target x86_64");

            // Compile the .ll to an exe.
            var exePath = Path.Combine(dir, Path.ChangeExtension(fileName, ".exe"));
            if (File.Exists(exePath) && overwrite == false)
            {
                var randName = Guid.NewGuid().ToString();
                exePath = Path.Combine(dir, randName);
            }


            RunClang(@$"""{asmPath}"" -target x86_64 -O3 -fno-vectorize -fno-slp-vectorize -O3  -mno-sse -c -o ""{exePath}""");

            // Return the compiled executable path.
            return exePath;
        }

        private static void RunClang(string arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = clangPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            process.Start();

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception("command failed.");
            }
        }
    }
}
