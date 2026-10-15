using System;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit7Checks
    {
        public static void Run()
        {
            string root = Path.Combine(Path.GetTempPath(), "Unit7Checks-" + Guid.NewGuid());
            Directory.CreateDirectory(Path.Combine(root, "Ru")); Directory.CreateDirectory(Path.Combine(root, "En"));
            try
            {
                File.WriteAllText(Path.Combine(root, "models.ini"), "[Interface]\r\nlang=Ru\r\nhelp=ru.pdf\r\n[lang]\r\ncount=3\r\nn1=Русский\r\nnn1=Ru\r\nhelp1=ru.pdf\r\nn2=English\r\nnn2=En\r\nhelp2=en.pdf\r\nn3=Chinese\r\nnn3=Cn\r\nhelp3=cn.pdf\r\n[Engine]\r\nname=sp-12\r\ncode=3\r\n", LegacyCodec.TextEncoding);
                File.WriteAllText(Path.Combine(root, "Ru", "menu.ini"), "[forms]\r\nn7=Language settings\r\n[Setups]\r\nl9=Language\r\nb1=Cancel\r\n[constants]\r\nn12=Restart required\r\n");
                File.WriteAllText(Path.Combine(root, "En", "menu.ini"), "[forms]\r\nn7=Settings\r\n");
                File.WriteAllText(Path.Combine(root, "Ru", "jobs.ini"), "[Engine]\r\ncount=1\r\nn1=sp-12\r\nnn1=3\r\n");
                string preferences = Path.Combine(root, "Preferences", "models.ini"); var settings = new ApplicationSettings(root, preferences);
                Check(settings.ChangeLanguage(settings.Languages[1]), "Unit7 language changes");
                var saved = IniFile.Load(preferences);
                Check(saved.Get("Interface", "lang") == "En" && saved.Get("Interface", "help") == "en.pdf", "Unit7 language and help saved together");
                Check(saved.Get("Engine", "name") == "sp-12" && saved.GetInt("Engine", "code") == 3, "Unit7 engine selection unchanged");
                Check(settings.Language == "Ru" && settings.PreferredLanguage == "En", "Unit7 active language unchanged until restart");
                settings.ApplyEngine(settings.GetEngines().Single());
                Check(IniFile.Load(preferences).Get("Interface", "lang") == "En", "Unit7 selecting engine preserves pending language");
                var reloaded = new ApplicationSettings(root, preferences); Check(reloaded.Language == "En", "Unit7 restart reads selected language");
                var before = File.ReadAllBytes(preferences);
                Check(!settings.ChangeLanguage(settings.Languages[1]) && File.ReadAllBytes(preferences).SequenceEqual(before), "Unit7 unchanged language does not rewrite settings");
                Check(IniFile.Load(Path.Combine(root, "models.ini")).Get("Interface", "lang") == "Ru", "Unit7 original catalog settings unchanged");
            }
            finally { Directory.Delete(root, true); }
        }
        private static void Check(bool condition, string label) { if (!condition) throw new Exception(label); Console.WriteLine("PASS " + label); }
    }
}
