using System;
using System.IO;

namespace Mdiapp.Core
{
    public static class CatalogWorkspace
    {
        // Seed missing files only: rebuilding the executable must not overwrite edited model catalogs.
        public static void Initialize(string bundledDirectory, string workingDirectory)
        {
            string source = Path.GetFullPath(bundledDirectory);
            string destination = Path.GetFullPath(workingDirectory);
            if (!Directory.Exists(source)) throw new DirectoryNotFoundException("Не найден каталог исходных настроек: " + source);
            CopyMissing(source, destination);
        }
        private static void CopyMissing(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.EnumerateFiles(source))
            {
                string target = Path.Combine(destination, Path.GetFileName(file));
                if (!File.Exists(target)) File.Copy(file, target);
            }
            foreach (string directory in Directory.EnumerateDirectories(source))
            {
                if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) continue;
                CopyMissing(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }
        }
    }
}
