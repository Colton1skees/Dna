using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Utilities
{
    public static class IDALoader
    {
        // TODO: Remove hardcoded path.
        private const string idaPath = @"C:\Program Files\IDA 7.5\ida64.exe";

        public static string Load(string exePath, bool overwrite = false)
        {
            // If the file already exists, we don't want to overwrite it.
            var dir = Path.GetDirectoryName(exePath);
            // Compile the .ll file to assembly with vectorization disabled.
            var fileName = Path.GetFileName(exePath);

            // Compile the .ll to an exe.
            if (File.Exists(exePath) && overwrite == false)
            {
                var randName = Guid.NewGuid().ToString();
                var newPath = Path.Combine(dir, randName);
                File.Move(exePath, newPath);
                exePath = newPath;
            }

            RunProcess($@"-A ""{exePath}""");

            // Return the compiled executable path.
            return exePath;
        }

        private static void RunProcess(string arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = idaPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            process.Start();

            process.WaitForExit(200);
            if (process.HasExited && process.ExitCode != 0)
            {
                throw new Exception("command failed.");
            }
        }
    }
}
