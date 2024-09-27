using AsmResolver.PE.File;
using Dna.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Binary.Windows
{
    public static class PEInjectorTest
    {
        public static void Test()
        {
            /*
            // Compile to a .exe using clang.
            Console.WriteLine("Compiling to an exe.");
            var compiledPath = ClangCompiler.Compile("translatedFunction.ll");

            Console.WriteLine("Loading into IDA.");
            var exePath = IDALoader.Load(compiledPath);
            */

            // Load the source binary to be injected.
            var srcPath = @"C:\Users\colton\source\repos\Dna\Dna.Example\bin\x64\Release\net7.0-windows\04c2369a-e97f-4459-bee9-676f2b47e5d7";
            var bytes = File.ReadAllBytes(srcPath);
            var srcBin = PEFile.FromBytes(bytes);

            // Load the target binary that's injected into.
            var dstPath = @"C:\Users\colton\Downloads\VMTarget.vmp.exe";
            var dstBin = PEFile.FromFile(dstPath);

            var injector = new PEInjector(srcBin, dstBin);
            injector.Inject();
        }
    }
}
