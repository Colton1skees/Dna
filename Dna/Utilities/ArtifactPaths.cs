using System;
using System.IO;

namespace Dna.Utilities
{
    public static class ArtifactPaths
    {
        private const string OutputDirectoryEnvVar = "DNA_OUTPUT_DIR";

        private static readonly Lazy<string> outputDirectory = new(() =>
        {
            var configured = Environment.GetEnvironmentVariable(OutputDirectoryEnvVar);
            var directory = !string.IsNullOrWhiteSpace(configured)
                ? configured
                : Path.Combine(FindRepositoryRoot(), "output");

            directory = Path.GetFullPath(directory);
            Directory.CreateDirectory(directory);
            return directory;
        });

        public static string OutputDirectory => outputDirectory.Value;

        public static string Resolve(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
                return path;

            var resolved = Path.Combine(OutputDirectory, path);
            var parent = Path.GetDirectoryName(resolved);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            return resolved;
        }

        public static string ResolveInputFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
                return path;

            var resolved = Resolve(path);
            if (!File.Exists(resolved) && File.Exists(path))
                File.Copy(path, resolved, overwrite: true);

            return resolved;
        }

        public static string CopyInputToOutput(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            var fullPath = Path.GetFullPath(path);
            if (IsInOutputDirectory(fullPath))
                return fullPath;

            var targetPath = GetAvailablePath(Path.Combine(OutputDirectory, Path.GetFileName(path)), overwrite: true);
            File.Copy(fullPath, targetPath, overwrite: true);
            return targetPath;
        }

        public static string GetAvailablePath(string path, bool overwrite)
        {
            if (overwrite || !File.Exists(path))
                return path;

            var directory = Path.GetDirectoryName(path) ?? OutputDirectory;
            var fileName = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            return Path.Combine(directory, $"{fileName}-{Guid.NewGuid():N}{extension}");
        }

        private static bool IsInOutputDirectory(string path)
        {
            var outputPath = OutputDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(outputPath, StringComparison.OrdinalIgnoreCase);
        }

        private static string FindRepositoryRoot()
        {
            foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                var directory = new DirectoryInfo(startPath);
                while (directory != null)
                {
                    if (File.Exists(Path.Combine(directory.FullName, "Dna.sln")))
                        return directory.FullName;

                    directory = directory.Parent;
                }
            }

            return Directory.GetCurrentDirectory();
        }
    }
}
