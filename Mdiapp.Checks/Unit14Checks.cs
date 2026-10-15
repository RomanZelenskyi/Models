using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit14Checks
    {
        public static void Run(string sourceRoot)
        {
            string root = Path.Combine(Path.GetTempPath(), "Unit14Checks-" + Guid.NewGuid()); Directory.CreateDirectory(root);
            try
            {
                var defs = new IniFile(); var menu = new IniFile(); menu.Set("files", "n16", "Group"); menu.Set("files", "n17", "Fourth");
                defs.Set("24", "ne1", "1"); defs.Set("24", "ne11", "True"); defs.Set("24", "ne13", "1"); defs.Set("24", "ne25", "1");
                string path = Path.Combine(root, "24.dat");
                File.WriteAllText(path, "24 Sample\r\n 1.25000-2.00000 0.00000\r\n 0.20000\r\n  0.14700000\r\n  1.70000005\r\n  0.04727000\r\n", LegacyCodec.TextEncoding);
                var model = InputFile24.Load(path, defs, menu, CultureInfo.GetCultureInfo("ru-RU"));
                Check(model.Values.SequenceEqual(new[] {1.25, -2, 0, 0.2}) && model.Enabled[0] && !model.Enabled[1], "Unit14 fixed fields and enabled flags");
                Check(model.AdditionalValues[0] == 0.147 && model.AdditionalValues[2] == 1.70000005 && model.AdditionalValues[14] == 0.04727, "Unit14 sparse rows including last read once");
                model.Save(path, CultureInfo.InvariantCulture);
                Check(File.ReadAllLines(path)[1] == " 1.25000-2.00000 0.00000        !Group" && File.ReadAllLines(path).Length == 6 && File.Exists(path + ".bak"), "Unit14 Delphi widths visible rows and backup");
                model.Save(path, CultureInfo.GetCultureInfo("ru-RU"));
                Check(InputFile24.Load(path, defs, menu, CultureInfo.GetCultureInfo("ru-RU")).AdditionalValues.SequenceEqual(model.AdditionalValues), "Unit14 decimal comma round trip");
                byte[] before = File.ReadAllBytes(path); model.Values[0] = 1000;
                try { model.Save(path); throw new Exception("Overflow accepted"); } catch (InvalidDataException) { Check(before.SequenceEqual(File.ReadAllBytes(path)), "Unit14 overflow leaves original unchanged"); }
                if (sourceRoot != null)
                {
                    string source = Path.Combine(sourceRoot, "sp-12", "data", "XKSM.dat");
                    var actualDefs = IniFile.Load(Path.Combine(sourceRoot, "sp-12", "Ru", "inputfiles.ini"));
                    var actual = InputFile24.Load(source, actualDefs, menu, CultureInfo.InvariantCulture);
                    Check(actual.Values[0] == 0.37181 && actual.AdditionalEnabled.Count(v => v) == 8 && actual.AdditionalValues[7] == 0.04727, "Unit14 original XKSM values including final row");
                    actual.Save(path, CultureInfo.InvariantCulture);
                    var copy = InputFile24.Load(path, actualDefs, menu, CultureInfo.InvariantCulture);
                    Check(copy.Values.SequenceEqual(actual.Values) && copy.AdditionalValues.SequenceEqual(actual.AdditionalValues), "Unit14 original data round trip");
                }
            }
            finally { Directory.Delete(root, true); }
        }
        private static void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); }
    }
}
