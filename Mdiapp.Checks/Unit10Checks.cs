using System;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit10Checks
    {
        public static void Run()
        {
            string root = Path.Combine(Path.GetTempPath(), "Unit10Checks-" + Guid.NewGuid()); Directory.CreateDirectory(Path.Combine(root, "Ru"));
            try
            {
                File.WriteAllText(Path.Combine(root, "models.ini"), "[Interface]\r\nlang=Ru\r\nhelp=manual.pdf\r\n[Engine]\r\nname=first\r\ncode=3\r\n");
                File.WriteAllText(Path.Combine(root, "Ru", "menu.ini"), "[forms]\r\nn3=Engine selection\r\n[Setups]\r\nt11=Engine\r\nb1=Cancel\r\n");
                string catalog = Path.Combine(root, "Ru", "jobs.ini");
                File.WriteAllText(catalog, "[Engine]\r\ncount=2\r\nn1=first\r\nnn1=3\r\nndescript1=First description\r\nn2=second\r\nnn2=17\r\nndescript2=Second description\r\n");
                string preferences = Path.Combine(root, "Preferences", "models.ini"); var settings = new ApplicationSettings(root, preferences);
                settings.ApplyEngine(settings.GetEngines().Single(e => e.Code == 17));
                Check(settings.Engine == "second", "Unit10 selected code is not row number");
                var saved = IniFile.Load(preferences);
                Check(saved.GetInt("Engine", "code") == 17 && saved.Get("Engine", "name") == "second", "Unit10 engine choice persists");
                Check(saved.Get("Interface", "lang") == "Ru" && saved.Get("Interface", "help") == "manual.pdf", "Unit10 language and help preserved");
            }
            finally { Directory.Delete(root, true); }
        }
        private static void Check(bool condition, string label) { if (!condition) throw new Exception(label); Console.WriteLine("PASS " + label); }
    }
}
