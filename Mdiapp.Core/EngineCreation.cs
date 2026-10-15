using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Mdiapp.Core
{
    public sealed class EngineCreationRequest
    {
        public string Name { get; set; }
        public int Code { get; set; }
        public string Description { get; set; }
        public string CalculationDirectory { get; set; }
        public string DataDirectory { get; set; }
        public string ParameterFile { get; set; }
        // The form normalizes the selected image to BMP before calling the service.
        public byte[] PictureBmp { get; set; }
        public string ProjectsDirectory { get; set; }
        public string ResultsDirectory { get; set; }
        public Dictionary<string, IniFile> ImportedSettings { get; set; }
    }
    public static class EngineCreation
    {
        public static string Create(string root, string language, EngineCreationRequest request, IProgress<int> progress = null, CultureInfo culture = null)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            root = Path.GetFullPath(root); ValidateName(language); ValidateName(request.Name);
            if (request.Code < 1 || request.Code > 100) throw new InvalidDataException("Код двигателя должен быть от 1 до 100.");
            if (request.Description != null && request.Description.IndexOfAny(new[] {'\r', '\n'}) >= 0) throw new InvalidDataException("Описание должно занимать одну строку.");
            if (request.PictureBmp != null && (request.PictureBmp.Length < 2 || request.PictureBmp[0] != 'B' || request.PictureBmp[1] != 'M')) throw new InvalidDataException("Выберите изображение двигателя.");
            string catalogPath = Path.Combine(root, language, "jobs.ini");
            if (!File.Exists(catalogPath)) throw new FileNotFoundException("Не найден каталог двигателей", catalogPath);
            var catalog = IniFile.Load(catalogPath); int count = catalog.GetInt("Engine", "count");
            if (count < 0 || count >= 100) throw new InvalidDataException("Каталог двигателей заполнен или повреждён.");
            for (int i = 1; i <= count; i++)
            {
                if (catalog.GetInt("Engine", "nn" + i) == request.Code) throw new InvalidDataException("Двигатель с таким кодом уже существует.");
                if (string.Equals(catalog.Get("Engine", "n" + i), request.Name, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Двигатель с таким именем уже существует.");
            }
            string destination = Path.Combine(root, request.Name);
            if (Directory.Exists(destination) || File.Exists(destination)) throw new IOException("Папка двигателя уже существует.");
            ValidateSource(request.CalculationDirectory, root, true); ValidateSource(request.DataDirectory, root, true);
            ValidateSource(request.ParameterFile, root, false);
            ValidateSource(request.ProjectsDirectory, root, true); ValidateSource(request.ResultsDirectory, root, true);
            if (request.ImportedSettings != null && request.ImportedSettings.Keys.Any(name => name != "jobs.ini" && name != "misc.ini" && name != "graphics.ini"))
                throw new InvalidDataException("Недопустимое имя файла настроек импорта.");
            string staging = Path.Combine(root, ".creating-" + Guid.NewGuid().ToString("N"));
            string catalogTemp = catalogPath + ".creating-" + Guid.NewGuid().ToString("N");
            bool moved = false, registered = false;
            try
            {
                foreach (string folder in new[] {"bin", "data", "datar", "result", language}) Directory.CreateDirectory(Path.Combine(staging, folder));
                progress?.Report(20);
                string local = Path.Combine(staging, language);
                var jobs = new IniFile();
                jobs.Set("jobs", "n_count", "1"); jobs.Set("jobs", "n1", "1"); jobs.Set("jobs", "p1", ""); jobs.Set("jobs", "f1", "1"); jobs.Set("jobs", "preg1", "");
                jobs.Set("configs", "key", request.Code.ToString(CultureInfo.InvariantCulture)); jobs.Set("configs", "configs", "1"); jobs.Set("configs", "tochnost", "0 1 2"); jobs.Set("configs", "Conditions", "0");
                culture = culture ?? CultureInfo.CurrentCulture;
                jobs.Set("configs", "t1", 0.00001.ToString("G", culture)); jobs.Set("configs", "t2", 0.000001.ToString("G", culture)); jobs.Set("configs", "t3", 0.1.ToString("G", culture));
                jobs.Set("configs", "ParameterNumbers1", "385"); jobs.Set("configs", "ParameterNumbers2", "1"); jobs.Set("configs", "CharacteristicNumbers", "1"); jobs.Set("configs", "time", "385"); jobs.Set("misc", "misc", "0");
                jobs.Save(Path.Combine(local, "jobs.ini"));
                var misc = new IniFile(); misc.Set("misc", "count", "1"); misc.Set("misc", "nn0", "0"); misc.Set("misc", "n0", "Miscellanea"); misc.Save(Path.Combine(local, "misc.ini"));
                if (request.ImportedSettings != null) foreach (var pair in request.ImportedSettings) pair.Value.Save(Path.Combine(local, pair.Key));
                progress?.Report(30);
                if (!string.IsNullOrWhiteSpace(request.ParameterFile)) File.Copy(request.ParameterFile, Path.Combine(local, "namepar1.txt"));
                else File.WriteAllText(Path.Combine(local, "namepar1.txt"), "-1\r\n", LegacyCodec.TextEncoding);
                if (request.PictureBmp != null) File.WriteAllBytes(Path.Combine(local, "PROJECT.BMP"), request.PictureBmp);
                progress?.Report(40);
                if (!string.IsNullOrWhiteSpace(request.CalculationDirectory)) CopyDirectory(request.CalculationDirectory, Path.Combine(staging, "bin"));
                progress?.Report(65);
                if (!string.IsNullOrWhiteSpace(request.DataDirectory)) CopyDirectory(request.DataDirectory, Path.Combine(staging, "data"));
                if (!string.IsNullOrWhiteSpace(request.ProjectsDirectory)) CopyDirectory(request.ProjectsDirectory, Path.Combine(staging, "datar"));
                if (!string.IsNullOrWhiteSpace(request.ResultsDirectory)) CopyDirectory(request.ResultsDirectory, Path.Combine(staging, "result"));
                progress?.Report(90);
                catalog.Set("Engine", "count", (count + 1).ToString()); catalog.Set("Engine", "n" + (count + 1), request.Name);
                catalog.Set("Engine", "nn" + (count + 1), request.Code.ToString()); catalog.Set("Engine", "ndescript" + (count + 1), request.Description ?? "");
                catalog.Save(catalogTemp);
                Directory.Move(staging, destination); moved = true;
                File.Replace(catalogTemp, catalogPath, catalogPath + ".bak"); registered = true;
                progress?.Report(100); return destination;
            }
            finally
            {
                if (File.Exists(catalogTemp)) File.Delete(catalogTemp);
                // Only directories created by this operation may be removed on failure.
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
                if (moved && !registered && Directory.Exists(destination)) Directory.Delete(destination, true);
            }
        }
        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".." || name.EndsWith(".") || name.EndsWith(" ") || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                Regex.IsMatch(name, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|$)", RegexOptions.IgnoreCase)) throw new InvalidDataException("Недопустимое имя папки двигателя или языка.");
        }
        private static void ValidateSource(string path, string root, bool directory)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            string source = Path.GetFullPath(path);
            if (!(directory ? Directory.Exists(source) : File.Exists(source))) throw new FileNotFoundException("Не найден источник копирования", source);
            if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0) throw new IOException("Источник не должен быть ссылкой на другую папку.");
            if (directory && (string.Equals(root, source.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase) || root.StartsWith(source.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)))
                throw new IOException("Источник копирования содержит каталог программы.");
        }
        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.EnumerateFiles(source))
            {
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new IOException("Нельзя копировать ссылку: " + file);
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), false);
            }
            foreach (string folder in Directory.EnumerateDirectories(source))
            {
                if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0) throw new IOException("Нельзя копировать ссылку: " + folder);
                CopyDirectory(folder, Path.Combine(destination, Path.GetFileName(folder)));
            }
        }
    }
}

