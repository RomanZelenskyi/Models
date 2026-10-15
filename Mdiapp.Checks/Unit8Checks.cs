using System;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit8Checks
    {
        public static void Run()
        {
            string root = Path.Combine(Path.GetTempPath(), "Unit8Checks-" + Guid.NewGuid()); Directory.CreateDirectory(Path.Combine(root, "Ru"));
            try
            {
                File.WriteAllText(Path.Combine(root, "models.ini"), "[Interface]\r\nlang=Ru\r\n");
                string path = Path.Combine(root, "Ru", "menu.ini");
                File.WriteAllText(path, ";comment\r\n[forms]\r\nn8=Export settings\r\n[ressave]\r\ntransp1=True\r\nish2=1\r\ngraph1=1\r\nformatindex2=9\r\ngraph3=True\r\ndigs1=4\r\n[unrelated]\r\nkeep=yes\r\n");
                var model = ResultExportSettings.Load(path);
                Check(model.Formats[0].Transpose && model.Formats[1].IncludeInput && model.Formats[2].IncludeGraphs && model.Formats[0].Digits == 4, "Unit8 reads Delphi booleans and precision");
                Check(!model.Formats[2].Transpose && model.Formats[2].Digits == 0, "Unit8 missing options default to false and zero");
                model.Formats[0].Transpose = false;
                model.Formats[0].FormatIndex = 2;
                model.Formats[0].Digits = 6;
                model.Formats[1].Digits = 0;
                model.Formats[2].Digits = 3;
                model.Formats[2].IncludeGraphs = false;
                model.Save(path);
                var saved = ResultExportSettings.Load(path);
                Check(!saved.Formats[0].Transpose && saved.Formats[0].FormatIndex == 2 && saved.Formats[0].Digits == 6 && saved.Formats[1].Digits == 0 && saved.Formats[2].Digits == 3 && !saved.Formats[2].IncludeGraphs, "Unit8 edits persist with Delphi invalid integer fallback");
                Check(saved.Formats[0].IncludeGraphs && saved.Formats[1].FormatIndex == 9, "Unit8 hidden format options retained");
                Check(IniFile.Load(path).Get("unrelated", "keep") == "yes" && File.ReadAllText(path).Contains(";comment") && File.Exists(path + ".bak"), "Unit8 menu sections comments and backup retained");
            }
            finally { Directory.Delete(root, true); }
        }
        private static void Check(bool condition, string label) { if (!condition) throw new Exception(label); Console.WriteLine("PASS " + label); }
    }
}
