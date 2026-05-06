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
        private static readonly string idaPath = FindIdaPath();

        private static string FindIdaPath()
        {
            var envPath = Environment.GetEnvironmentVariable("IDA_PATH");
            if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
                return envPath;

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles) && Directory.Exists(programFiles))
            {
                foreach (var idaDir in Directory.EnumerateDirectories(programFiles, "IDA*"))
                {
                    var idaPath = Path.Combine(idaDir, "ida64.exe");
                    if (File.Exists(idaPath))
                        return idaPath;
                }
            }

            return "ida64.exe";
        }

        public static string Load(string exePath, bool overwrite = true)
        {
            exePath = ArtifactPaths.CopyInputToOutput(ArtifactPaths.ResolveInputFile(exePath));

            // If the database already exists, use a unique copy so IDA does not
            // reuse/overwrite an old .i64 next to the input.
            if (File.Exists(exePath) && overwrite == false)
            {
                var dir = Path.GetDirectoryName(exePath) ?? ArtifactPaths.OutputDirectory;
                var fileName = Path.GetFileNameWithoutExtension(exePath);
                var extension = Path.GetExtension(exePath);
                var newPath = Path.Combine(dir, $"{fileName}-{Guid.NewGuid():N}{extension}");
                File.Copy(exePath, newPath, overwrite: false);
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
