using System;
using System.IO;

namespace Mdiapp.Core
{
    public enum ResultFileFormat { Text = 1, Html = 2, Native = 3 }
    public sealed class ResultFileSelection
    {
        public ResultFileFormat Format { get; }
        public string FileName { get; }
        public ResultFileSelection(ResultFileFormat format, string fileName)
        {
            Extension(format);
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Укажите имя файла результатов.");
            Format = format; FileName = Path.GetFullPath(fileName);
        }
        public static string Extension(ResultFileFormat format)
        {
            switch (format) { case ResultFileFormat.Text: return ".txt"; case ResultFileFormat.Html: return ".htm"; case ResultFileFormat.Native: return ".res"; default: throw new ArgumentOutOfRangeException(nameof(format)); }
        }
        public static string Suggest(string projectPath, string programDirectory, string engine, ResultFileFormat format)
        {
            string extension = Extension(format);
            if (string.IsNullOrWhiteSpace(projectPath) || projectPath.StartsWith("NONAME", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(engine) || engine == "." || engine == ".." || engine.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new ArgumentException("Не выбран двигатель.");
                projectPath = Path.Combine(programDirectory, engine, "datar", "NONAME.inm");
            }
            string fullPath = Path.GetFullPath(projectPath);
            string directory = Path.GetDirectoryName(fullPath), parent = directory;
            // Replace a directory component rather than any substring named DATAR.
            while (!string.IsNullOrEmpty(parent))
            {
                if (string.Equals(Path.GetFileName(parent), "datar", StringComparison.OrdinalIgnoreCase))
                {
                    string suffix = directory.Substring(parent.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string result = Path.Combine(Path.GetDirectoryName(parent), "Result", suffix, Path.GetFileNameWithoutExtension(fullPath) + extension);
                    return result;
                }
                string next = Path.GetDirectoryName(parent); if (next == parent) break; parent = next;
            }
            return Path.Combine(directory, "Result", Path.GetFileNameWithoutExtension(fullPath) + extension);
        }
    }
}
