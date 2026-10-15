using System;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit3Checks
    {
        public static void Run(string sourceRoot)
        {
            var root = Path.Combine(Path.GetTempPath(), "Unit3Checks-" + Guid.NewGuid());
            try
            {
                CopyDirectory(Path.Combine(sourceRoot, "Ru"), Path.Combine(root, "Ru"));
                CopyDirectory(Path.Combine(sourceRoot, "sp-12", "Ru"), Path.Combine(root, "sp-12", "Ru"));
                File.Copy(Path.Combine(sourceRoot, "models.ini"), Path.Combine(root, "models.ini"));
                var original = File.ReadAllBytes(Path.Combine(sourceRoot, "sp-12", "Ru", "jobs.ini"));
                var model = new Unit3Model(root, "Ru", "sp-12");
                Check(model.Engines.Single().Code == 3 && model.Engines.Single().Name == "sp-12", "Unit3 real engine ID");
                Check(model.Parameters.Count > 300 && model.Parameters.Single(p => p.Number == 1).Symbol == "P*_1", "Unit3 real parameter catalog");
                Check(model.Characteristics.Count > 100 && model.Includes.Any(f => f.Number == 51), "Unit3 characteristics and include types");
                Check(model.Jobs.Any(j => j.Number == 4 && j.Enabled && j.Executable == "IdentIntrf_ru.exe"), "Unit3 sparse job mapping");
                Check(model.IncludeOrder.Contains(1210), "Unit3 include IDs are not row indexes");
                model.EngineJobs.Set("Unported", "Keep", "Сохранить");
                model.SelectedParameters.Clear(); model.TimeParameter = 385;
                model.SelectedParameters.Add(1); model.SelectedParameters.Add(20);
                model.SelectedControls.Clear();
                model.SelectedCharacteristics.Clear(); model.SelectedCharacteristics.Add(7);
                model.SelectedFile30.Clear(); model.SelectedFile30.Add(2);
                model.IncludeOrder.Clear(); model.IncludeOrder.Add(51); model.IncludeOrder.Add(1);
                model.File3132.Clear(); model.File3132.Add(new Unit3OrderedParameter { Number = 0, Enabled = true });
                model.File3132.Add(new Unit3OrderedParameter { Number = 20, Enabled = true });
                model.Graphs.Clear(); model.Graphs.Add(new Unit3Graph { n = 999, Key_X = 20, ParKey = 1 });
                model.Graphs.Add(new Unit3Graph { n = 1000 });
                model.DynamicFactors[4].Clear(); model.DynamicFactors[4].Add("Второй"); model.DynamicFactors[4].Add("Первый");
                model.InputFiles.Set("41", "n69", "Последнее поле"); model.InputFiles.Set("24", "ne25", "1");
                model.Save();
                var loaded = new Unit3Model(root, "Ru", "sp-12");
                Check(loaded.SelectedParameters.SetEquals(new[] {385, 1, 20}), "Unit3 time first and split parameter keys");
                Check(loaded.SelectedControls.Count == 0, "Unit3 empty controls do not reuse uninitialized variable");
                Check(loaded.SelectedCharacteristics.SetEquals(new[] {7}) && loaded.SelectedFile30.SetEquals(new[] {2}), "Unit3 characteristic and file30 selections");
                Check(loaded.IncludeOrder.SequenceEqual(new[] {51, 1}), "Unit3 include order preserved");
                Check(Enumerable.Range(1, loaded.EngineJobs.GetInt("jobs", "n_count")).All(i => loaded.EngineJobs.Get("jobs", "f" + i) == "51 1"), "Unit3 common include order written to every enabled job");
                Check(loaded.File3132.Count == 2 && loaded.File3132[0].Number == 0 && loaded.File3132[1].Number == 20 && loaded.File3132[1].Enabled, "Unit3 31-32 reserve and order");
                Check(loaded.Graphs.Count == 2 && loaded.Graphs[0].n == 999 && loaded.Graphs[0].Key_X == 20, "Unit3 graphics fields and count");
                Check(loaded.DynamicFactors[4].SequenceEqual(new[] {"Второй", "Первый"}), "Unit3 dynamics order and Cyrillic");
                Check(loaded.InputFiles.Get("41", "n69") == "Последнее поле" && loaded.InputFiles.Get("24", "ne25") == "1", "Unit3 input-file keys");
                Check(loaded.EngineJobs.Get("Unported", "Keep") == "Сохранить", "Unit3 unknown INI sections retained");
                string globalOriginal = File.ReadAllText(Path.Combine(root, "Ru", "jobs.ini.bak"), LegacyCodec.TextEncoding);
                string originalComment = globalOriginal.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None).First(line => line.StartsWith(";"));
                Check(File.ReadAllText(Path.Combine(root, "Ru", "jobs.ini"), LegacyCodec.TextEncoding).Contains(originalComment), "Unit3 INI comments retained");
                Check(File.Exists(Path.Combine(root, "sp-12", "Ru", "jobs.ini.bak")), "Unit3 backup created");
                foreach (string path in new[] {loaded.GlobalPath("jobs.ini"), loaded.EnginePath("jobs.ini")})
                {
                    using var coded = File.OpenRead(path + ".cod"); using var decoded = new MemoryStream(); LegacyCodec.Decode(coded, decoded);
                    Check(decoded.ToArray().SequenceEqual(File.ReadAllBytes(path)), "Unit3 jobs COD matches INI");
                }
                var snapshot = File.ReadAllBytes(loaded.EnginePath("jobs.ini"));
                loaded.Engines.Add(new Unit3Engine { Code = 3, Name = "duplicate" });
                try { loaded.Save(); throw new Exception("Duplicate accepted"); }
                catch (InvalidDataException) { Check(snapshot.SequenceEqual(File.ReadAllBytes(loaded.EnginePath("jobs.ini"))), "Unit3 validation precedes file writes"); }
                var split = new IniFile(); Unit3Model.WritePair(split, "ParameterNumbers", Enumerable.Range(1, 2000));
                var first = Unit3Model.ParseNumbers(split.Get("configs", "ParameterNumbers1"), 1, 2000);
                var second = Unit3Model.ParseNumbers(split.Get("configs", "ParameterNumbers2"), 1, 2000);
                Check(first.Concat(second).SequenceEqual(Enumerable.Range(1, 2000)), "Unit3 2000 parameter IDs survive key split");
                Check(original.SequenceEqual(File.ReadAllBytes(Path.Combine(sourceRoot, "sp-12", "Ru", "jobs.ini"))), "Unit3 original Delphi data unchanged");
                string seeded = Path.Combine(root, "Seeded"); CatalogWorkspace.Initialize(Path.Combine(sourceRoot, "Ru"), seeded);
                string seededIni = Path.Combine(seeded, "jobs.ini"); File.WriteAllText(seededIni, "edited");
                CatalogWorkspace.Initialize(Path.Combine(sourceRoot, "Ru"), seeded);
                Check(File.ReadAllText(seededIni) == "edited", "Unit3 rebuilding does not overwrite user catalog");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (var file in Directory.EnumerateFiles(source))
                if (new[] {".ini", ".txt"}.Contains(Path.GetExtension(file).ToLowerInvariant())) File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        }
        private static void Check(bool condition, string name)
        { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); }
    }
}
