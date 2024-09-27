using Dna.Extensions;
using Dna.LLVMInterop.API.Remill.BC;
using LLVMSharp.Interop;
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
        private const string clangPath = @"C:\Users\colton\source\repos\cxx-common-cmake-win\cxx-common-cmake\build\install\bin\clang.exe";

        // TODO: Remove hardcoded path.
        private const string objcpyPath = @"C:\Users\colton\source\repos\cxx-common-cmake-win\cxx-common-cmake\build\install\bin\llvm-objcopy.exe";

        // TODO: Remove hardcoded path.
        private const string optPath = @"C:\Users\colton\source\repos\cxx-common-cmake-win\cxx-common-cmake\build\install\bin\opt.exe";

        public static unsafe string CompileToWindowsDll(LLVMValueRef targetFunction, string llPath, bool overwrite = false)
        {
            targetFunction.GlobalParent.WriteToLlFile(llPath);



            var lines = File.ReadAllLines(llPath).ToList();
            lines.Add(Environment.NewLine);
            lines.Add(@"
define dllexport i32 @_DllMainCRTStartup() {
	entry:
	ret i32 0
}

define i32 @main(i32 %argc, i8** %argv)
{
	entry:
	ret i32 0
}
");
            File.WriteAllLines(llPath, lines);
            var targetLine = lines.Single(x => x.Contains(targetFunction.Name) && x.Contains("define"));
            var targetIndex = lines.IndexOf(targetLine);
            lines[targetIndex] = targetLine.Replace("define ", "define dllexport ");

            // Write the patched code.
            File.WriteAllLines(llPath, lines);

            /*
            foreach(var function in module.GetFunctions())
            {
                function.
            }
          
            module.WriteToLlFile(llPath);
              */
            // If the file already exists, we don't want to overwrite it.
            var dir = Path.GetDirectoryName(llPath);
            // Compile the .ll file to assembly with vectorization disabled.
            var fileName = Path.GetFileName(llPath);
            var asmPath = Path.Combine(dir, Path.ChangeExtension(fileName, ".asm"));
            RunClang(clangPath, @$"""{llPath}"" -S -o ""{asmPath}"" -fno-vectorize -fno-slp-vectorize -mno-avx -mno-avx512f -O3 -fasync-exceptions -fseh-exceptions -fexceptions -fcxx-exceptions -mno-sse -target x86_64-pc-windows-msvc");


            // Compile the .ll to an exe.
            var objPath = Path.Combine(dir, Path.ChangeExtension(fileName, ".exe"));
            if (File.Exists(objPath) && overwrite == false)
            {
                var randName = Guid.NewGuid().ToString();
                objPath = Path.Combine(dir, randName);
            }

            RunClang(clangPath, @$"""{asmPath}"" -target x86_64-pc-windows-msvc -O3 -mno-avx -mno-avx512f -fno-vectorize -fno-slp-vectorize -O3 -fasync-exceptions -fseh-exceptions -fexceptions -fcxx-exceptions -mno-sse -shared -o ""{objPath}""");

            //var exePath = Path.Combine(dir, Path.ChangeExtension(fileName, ".exe"));
            //RunClang(objcpyPath, @$" --input-target=coff-x86-64 --output-target=pe-x86-64 ""{objPath}"" ""{exePath}"" ");

            // Return the compiled executable path.
            return objPath;
        }

        public static unsafe string Compile(string llPath, bool overwrite = false)
        {
            /*
            foreach(var function in module.GetFunctions())
            {
                function.
            }
          
            module.WriteToLlFile(llPath);
              */
            // If the file already exists, we don't want to overwrite it.
            var dir = Path.GetDirectoryName(llPath);
            // Compile the .ll file to assembly with vectorization disabled.
            var fileName = Path.GetFileName(llPath);
            var asmPath = Path.Combine(dir, Path.ChangeExtension(fileName, ".asm"));
            RunClang(clangPath, @$"""{llPath}"" -S -o ""{asmPath}"" -fno-vectorize -fno-slp-vectorize -O3 -fasync-exceptions -fseh-exceptions -fexceptions -fcxx-exceptions -mno-sse -target x86_64-pc-windows-msvc");


            // Compile the .ll to an exe.
            var objPath = Path.Combine(dir, Path.ChangeExtension(fileName, ".exe"));
            if (File.Exists(objPath) && overwrite == false)
            {
                var randName = Guid.NewGuid().ToString();
                objPath = Path.Combine(dir, randName);
            }

            RunClang(clangPath, @$"""{asmPath}"" -target x86_64-pc-windows-msvc -O3 -fasync-exceptions -fseh-exceptions -fexceptions -fcxx-exceptions -fno-vectorize -fno-slp-vectorize -c -mno-sse -o ""{objPath}""");

            //var exePath = Path.Combine(dir, Path.ChangeExtension(fileName, ".exe"));
            //RunClang(objcpyPath, @$" --input-target=coff-x86-64 --output-target=pe-x86-64 ""{objPath}"" ""{exePath}"" ");

            // Return the compiled executable path.
            return objPath;
        }

        private static void RunClang(string exePath, string arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            process.Start();

            Console.WriteLine("");
            Console.WriteLine($@"""{exePath}"" {arguments}");
                process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception("command failed.");
            }
        }

        public static LLVMModuleRef Optimize(LLVMModuleRef module, string llPath, bool overwrite = false)
        {
            module.WriteToLlFile(llPath);

            // If the file already exists, we don't want to overwrite it.
            var dir = Path.GetDirectoryName(llPath);
            // Compile the .ll file to assembly with vectorization disabled.
            var fileName = Path.GetFileName(llPath);
            var newPath = Path.Combine(dir, Path.ChangeExtension(fileName, ".opt.ll"));

            RunClang(optPath, @$"-O3 -S ""{llPath}"" -o {newPath}");

            return RemillUtils.LoadModuleFromFile(module.Context, Path.Combine(Directory.GetCurrentDirectory(), newPath)).Value;
        }
    }
}
