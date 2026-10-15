using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit11Checks
    {
        public static void Run(string sourceRoot)
        {
            string root = Path.Combine(Path.GetTempPath(), "Unit11Checks-" + Guid.NewGuid()); Directory.CreateDirectory(root);
            try
            {
                var definitions = new IniFile(); definitions.Set("31_32", "count", "3");
                definitions.Set("31_32", "n1", "0"); definitions.Set("31_32", "e1", "1");
                definitions.Set("31_32", "n2", "1"); definitions.Set("31_32", "e2", "1");
                definitions.Set("31_32", "n3", "0"); definitions.Set("31_32", "e3", "0");
                definitions.Set("31_32", "preg2", "1");
                string first = Path.Combine(root, "31.dat"), second = Path.Combine(root, "32.dat");
                File.WriteAllText(first, "31 Initial\r\n      9.000000      !reserve\r\n      2.000000      !p\r\nPREG title\r\n1          !nreg\r\n3.5          !PREG 2\r\n0\r\n      1.000000       2.000000       3.000000\r\n");
                File.WriteAllText(second, "32 Bounds\r\n      9.000000      10.000000\r\n      1.000000       4.000000\r\n");
                var catalog = new[] {new Unit3Parameter { Number = 1, Symbol = "p", Name = "Parameter" }};
                var model = InputFiles3132.Load(first, second, definitions, catalog, "PREG title", CultureInfo.InvariantCulture);
                Check(model.Rows.Count == 2 && model.Divider == 2 && model.Rows[0].Value == 0.5 && model.Rows[0].Minimum == 0 && model.Rows[0].Maximum == 1, "Unit11 enabled reserve defaults and skipped disabled reserves");
                Check(model.Preg.Single().Code == 2 && model.Preg.Single().Parameter == 1 && model.Preg.Single().Value == 3.5, "Unit11 sparse PREG identity");
                Check(model.ConditionValues.SequenceEqual(new[] {1d, 2, 3}), "Unit11 condition values read");
                model.Save(first, second, CultureInfo.InvariantCulture);
                string[] rows = File.ReadAllLines(first); Check(rows[1].Substring(0, 14) == "      0.500000" && rows[2].Contains("!p Parameter"), "Unit11 fixed width and comments");
                var loaded = InputFiles3132.Load(first, second, definitions, catalog, "PREG title", CultureInfo.InvariantCulture);
                Check(loaded.Rows[1].Value == 2 && loaded.Rows[1].Minimum == 1 && loaded.Rows[1].Maximum == 4 && loaded.Preg.Single().Value == 3.5, "Unit11 paired files round trip");
                Check(File.Exists(first + ".bak") && File.Exists(second + ".bak"), "Unit11 backups for both files");
                byte[] original = File.ReadAllBytes(first); model.Rows[1].Value = 5;
                ExpectFailure(() => model.Save(first, second), "Unit11 upper bound violation rejected");
                Check(original.SequenceEqual(File.ReadAllBytes(first)), "Unit11 invalid values do not modify files");
                model.Rows[1].Value = 0; ExpectFailure(() => model.Save(first, second), "Unit11 lower bound violation rejected"); model.Rows[1].Value = 2;
                ExpectFailure(() => model.Save(first, first), "Unit11 same output filename rejected");
                model.Comment31 = "Changed";
                using (var locked = new FileStream(second, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    ExpectFailure(() => model.Save(first, second), "Unit11 second file write failure reported");
                Check(original.SequenceEqual(File.ReadAllBytes(first)) && !Directory.EnumerateFiles(root, "*.new-*").Any(), "Unit11 restores first file after second file failure");
                if (!string.IsNullOrEmpty(sourceRoot))
                {
                    var engine = new Unit3Model(sourceRoot, "Ru", "sp-12");
                    string actual31 = Path.Combine(sourceRoot, "sp-12", "data", "nezper0.DAT"), actual32 = Path.Combine(sourceRoot, "sp-12", "data", "Prednp.dat");
                    var actual = InputFiles3132.Load(actual31, actual32, engine.InputFiles, engine.Parameters, "PREG", CultureInfo.InvariantCulture);
                    Check(actual.Rows.Count == 21 && actual.Rows[0].Number == 186 && actual.Preg.Single().Parameter == 425 && actual.Preg.Single().Value == 4500, "Unit11 original SP-12 pair reads correctly");
                    string copy31 = Path.Combine(root, "real31.dat"), copy32 = Path.Combine(root, "real32.dat");
                    actual.Save(copy31, copy32, CultureInfo.InvariantCulture);
                    var realRoundTrip = InputFiles3132.Load(copy31, copy32, engine.InputFiles, engine.Parameters, "PREG", CultureInfo.InvariantCulture);
                    Check(realRoundTrip.Rows.Select(r => r.Value).SequenceEqual(actual.Rows.Select(r => r.Value)) && realRoundTrip.Preg.Single().Parameter == 425, "Unit11 real SP-12 values survive saving pair");
                }
            }
            finally { Directory.Delete(root, true); }
        }
        private static void ExpectFailure(Action action, string label)
        { try { action(); } catch (InvalidDataException) { Console.WriteLine("PASS " + label); return; } catch (IOException) { Console.WriteLine("PASS " + label); return; } throw new Exception(label); }
        private static void Check(bool condition, string label) { if (!condition) throw new Exception(label); Console.WriteLine("PASS " + label); }
    }
}
