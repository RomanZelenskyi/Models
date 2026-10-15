using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit4Checks
    {
        private sealed class ProgressLog : IProgress<int>
        {
            public List<int> Values { get; } = new List<int>();
            public void Report(int value) => Values.Add(value);
        }
        public static void Run()
        {
            string root = Path.Combine(Path.GetTempPath(), "Unit4Checks-" + Guid.NewGuid());
            Directory.CreateDirectory(Path.Combine(root, "Ru"));
            try
            {
                string catalogPath = Path.Combine(root, "Ru", "jobs.ini");
                File.WriteAllText(catalogPath, ";Комментарий\r\n[Engine]\r\ncount=1\r\nn1=existing\r\nnn1=3\r\nndescript1=Существующий\r\n[Other]\r\nkeep=yes\r\n", LegacyCodec.TextEncoding);
                string bin = Path.Combine(root, "Sources", "bin"), data = Path.Combine(root, "Sources", "data");
                Directory.CreateDirectory(Path.Combine(bin, "nested")); Directory.CreateDirectory(data);
                File.WriteAllBytes(Path.Combine(bin, "calc.exe"), new byte[] {1,2,3,4});
                File.WriteAllText(Path.Combine(bin, "nested", "extra.dat"), "nested");
                File.WriteAllText(Path.Combine(data, "input.dat"), "51 sample\r\n", LegacyCodec.TextEncoding);
                byte[] bmp;
                using (var image = new System.Drawing.Bitmap(2, 2))
                using (var output = new MemoryStream()) { image.Save(output, System.Drawing.Imaging.ImageFormat.Bmp); bmp = output.ToArray(); }
                var request = new EngineCreationRequest { Name = "new-engine", Code = 17, Description = "Новый двигатель", CalculationDirectory = bin, DataDirectory = data, PictureBmp = bmp };
                var progress = new ProgressLog();
                string created = EngineCreation.Create(root, "Ru", request, progress, CultureInfo.InvariantCulture);
                Check(new[] {"bin", "data", "datar", "result", "Ru"}.All(folder => Directory.Exists(Path.Combine(created, folder))), "Unit4 directory structure");
                Check(File.ReadAllBytes(Path.Combine(created, "bin", "calc.exe")).SequenceEqual(new byte[] {1,2,3,4}) && File.ReadAllText(Path.Combine(created, "bin", "nested", "extra.dat")) == "nested", "Unit4 recursive binary copy");
                Check(File.ReadAllText(Path.Combine(created, "data", "input.dat"), LegacyCodec.TextEncoding) == "51 sample\r\n", "Unit4 data copy");
                Check(File.ReadAllBytes(Path.Combine(created, "Ru", "PROJECT.BMP")).SequenceEqual(bmp), "Unit4 image copied without optional parameters");
                Check(File.ReadAllText(Path.Combine(created, "Ru", "namepar1.txt"), LegacyCodec.TextEncoding) == "-1\r\n", "Unit4 default parameter file");
                var jobs = IniFile.Load(Path.Combine(created, "Ru", "jobs.ini"));
                Check(jobs.GetInt("configs", "key") == 17 && jobs.Get("configs", "ParameterNumbers1") == "385" && jobs.Get("configs", "ParameterNumbers2") == "1" && jobs.Get("jobs", "f1") == "1", "Unit4 Delphi initial settings");
                Check(double.Parse(jobs.Get("configs", "t1"), CultureInfo.InvariantCulture) == 0.00001, "Unit4 initial accuracy");
                Check(IniFile.Load(Path.Combine(created, "Ru", "misc.ini")).Get("misc", "n0") == "Miscellanea", "Unit4 corrected misc n0 key");
                var catalog = IniFile.Load(catalogPath);
                Check(catalog.GetInt("Engine", "count") == 2 && catalog.GetInt("Engine", "nn2") == 17 && catalog.Get("Engine", "ndescript2") == "Новый двигатель", "Unit4 engine registration");
                Check(catalog.Get("Other", "keep") == "yes" && File.ReadAllText(catalogPath, LegacyCodec.TextEncoding).Contains(";Комментарий") && File.Exists(catalogPath + ".bak"), "Unit4 preserves catalog and backup");
                Check(progress.Values.SequenceEqual(new[] {20,30,40,65,90,100}), "Unit4 progress milestones");
                var snapshot = File.ReadAllBytes(catalogPath);
                ExpectFailure(() => EngineCreation.Create(root, "Ru", request), "Unit4 duplicate name/code rejected");
                request.Name = "../escape"; request.Code = 18; ExpectFailure(() => EngineCreation.Create(root, "Ru", request), "Unit4 invalid folder rejected");
                request.Name = "reserved"; request.Code = 101; ExpectFailure(() => EngineCreation.Create(root, "Ru", request), "Unit4 out-of-range code rejected");
                request.Name = "failed-engine"; request.Code = 18;
                using (var locked = new FileStream(Path.Combine(bin, "calc.exe"), FileMode.Open, FileAccess.Read, FileShare.None))
                    ExpectFailure(() => EngineCreation.Create(root, "Ru", request), "Unit4 copy failure reported");
                Check(snapshot.SequenceEqual(File.ReadAllBytes(catalogPath)) && !Directory.Exists(Path.Combine(root, "failed-engine")) && !Directory.EnumerateDirectories(root, ".creating-*").Any(), "Unit4 failed copy leaves no registered or partial engine");
                request.Name = "with-parameters"; request.Code = 18;
                request.ParameterFile = Path.Combine(root, "Sources", "parameters.txt"); File.WriteAllText(request.ParameterFile, "1 p parameter\r\n-1\r\n", LegacyCodec.TextEncoding);
                var second = EngineCreation.Create(root, "Ru", request, culture: CultureInfo.GetCultureInfo("ru-RU"));
                Check(File.ReadAllBytes(request.ParameterFile).SequenceEqual(File.ReadAllBytes(Path.Combine(second, "Ru", "namepar1.txt"))), "Unit4 optional parameter file copied");
                Check(double.Parse(IniFile.Load(Path.Combine(second, "Ru", "jobs.ini")).Get("configs", "t3"), CultureInfo.GetCultureInfo("ru-RU")) == 0.1, "Unit4 decimal comma compatibility");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
        private static void ExpectFailure(Action action, string name)
        {
            try { action(); }
            catch (InvalidDataException) { Console.WriteLine("PASS " + name); return; }
            catch (IOException) { Console.WriteLine("PASS " + name); return; }
            throw new Exception(name);
        }
        private static void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); }
    }
}
