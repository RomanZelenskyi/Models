using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit13Checks
    {
        public static void Run(string sourceRoot)
        {
            if (sourceRoot == null) return;
            string root = Path.Combine(Path.GetTempPath(), "Unit13Checks-" + Guid.NewGuid()); Directory.CreateDirectory(root);
            try
            {
                var definitions = IniFile.Load(Path.Combine(sourceRoot, "sp-12", "Ru", "inputfiles.ini"));
                foreach (var pair in new[] {new {File = "Xotbor1.dat", Type = 28}, new {File = "Xotbor2.dat", Type = 281}, new {File = "Xotbor3.dat", Type = 282}})
                {
                    string source = Path.Combine(sourceRoot, "sp-12", "data", pair.File); byte[] before = File.ReadAllBytes(source);
                    var model = InputFile28.Load(source, definitions, CultureInfo.InvariantCulture);
                    Check(model.Type == pair.Type && model.Rows.Count == 5 && model.Columns.Count == 10, "Unit13 original type " + pair.Type + " and visible matrix");
                    Check(model.Columns.Select(c => c.PhysicalIndex).SequenceEqual(new[] {0, 12, 13, 14, 1, 2, 3, 7, 8, 9}), "Unit13 nrow mapping " + pair.Type);
                    string target = Path.Combine(root, pair.File); model.Save(target, CultureInfo.InvariantCulture);
                    var loaded = InputFile28.Load(target, definitions, CultureInfo.InvariantCulture);
                    Check(loaded.Type == pair.Type && model.Rows.All(r => model.Columns.All(c => model.Values[r.PhysicalIndex, c.PhysicalIndex] == loaded.Values[r.PhysicalIndex, c.PhysicalIndex])), "Unit13 visible values and type survive saving " + pair.Type);
                    Check(loaded.Power.SequenceEqual(model.Power) && loaded.TrailingLines.SequenceEqual(model.TrailingLines), "Unit13 power and trailing data retained " + pair.Type);
                    Check(File.ReadAllLines(target, LegacyCodec.TextEncoding)[2].Substring(0, 9) == "  0.00000", "Unit13 disabled physical row cleared " + pair.Type);
                    Check(before.SequenceEqual(File.ReadAllBytes(source)), "Unit13 original unchanged " + pair.Type);
                    if (pair.Type == 28)
                    {
                        var snapshot = File.ReadAllBytes(target); model.Values[model.Rows[0].PhysicalIndex, model.Columns[0].PhysicalIndex] = 123456789;
                        try { model.Save(target); throw new Exception("Overflow accepted"); } catch (InvalidDataException) { Check(snapshot.SequenceEqual(File.ReadAllBytes(target)), "Unit13 field overflow prevents write"); }
                    }
                }
                var extended = new IniFile(); extended.Set("28", "n28count", "12"); extended.Set("28", "ce1", "1"); extended.Set("28", "nrow1", "1"); extended.Set("28", "re12", "1");
                string custom = Path.Combine(root, "extended.dat");
                var lines = new System.Collections.Generic.List<string> {"28 Extended", new string(' ', 135)};
                for (int i = 0; i < 12; i++) lines.Add(string.Concat(Enumerable.Range(0, 15).Select(col => ((i + 1) / 100d).ToString("F5", CultureInfo.InvariantCulture).PadLeft(9))));
                lines.Add(string.Concat(Enumerable.Repeat("        0.00000", 3))); File.WriteAllLines(custom, lines, LegacyCodec.TextEncoding);
                var extra = InputFile28.Load(custom, extended, CultureInfo.InvariantCulture); extra.Save(custom, CultureInfo.InvariantCulture);
                Check(InputFile28.Load(custom, extended, CultureInfo.InvariantCulture).Values[11, 0] == 0.12, "Unit13 enabled row beyond ten retained");
            }
            finally { Directory.Delete(root, true); }
        }
        private static void Check(bool condition, string label) { if (!condition) throw new Exception(label); Console.WriteLine("PASS " + label); }
    }
}
