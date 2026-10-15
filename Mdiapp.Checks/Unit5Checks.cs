using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit5Checks
    {
        public static void Run()
        {
            var profile = new LegacyEngineProfile { Name = "old-engine" };
            profile.Values["Parameter numbers"] = "385 1 20 2000"; profile.Values["Characteristic numbers"] = "1 7";
            profile.Values["Accuracies"] = "0 1 2"; profile.Values["Input conditions"] = "3";
            profile.Jobs[1] = "calc.exe/51 1210/0 2"; profile.Jobs[7] = "other.exe/31 32/1";
            profile.Configurations.Add(0); profile.Configurations.Add(3); profile.Misc[2] = "Разное"; profile.Misc[17] = "Другой пункт";
            var graph = EngineImport.GraphKeys.Values.ToDictionary(key => key, key => 42);
            graph["Par4"] = 9; graph["Key (X)"] = 385; profile.Graphs[777] = graph;
            var settings = EngineImport.ConvertProfile(profile, 19);
            var jobs = settings["jobs.ini"];
            Check(jobs.GetInt("jobs", "n_count") == 2 && jobs.GetInt("jobs", "n2") == 7 && jobs.Get("jobs", "p2") == "other.exe", "Unit5 sparse jobs converted");
            Check(jobs.Get("jobs", "f1") == "51 1210" && jobs.Get("jobs", "preg1") == "0 2", "Unit5 jobs slash format");
            Check(jobs.Get("configs", "configs") == "0 3" && jobs.GetInt("configs", "key") == 19, "Unit5 configuration IDs and new engine key");
            Check(jobs.Get("configs", "ParameterNumbers1") == "385 1 20" && jobs.Get("configs", "ParameterNumbers2") == "2000", "Unit5 split handles four-digit last parameter");
            Check(jobs.Get("configs", "Conditions") == "0" && jobs.GetInt("configs", "time") == 385, "Unit5 Delphi condition and time defaults");
            Check(settings["misc.ini"].GetInt("misc", "count") == 2 && settings["misc.ini"].GetInt("misc", "nn1") == 17 && settings["misc.ini"].Get("misc", "n1") == "Другой пункт", "Unit5 miscellaneous sparse IDs");
            Check(settings["graphics.ini"].GetInt("graph1", "n") == 777 && settings["graphics.ini"].GetInt("graph1", "Par") == 9 && settings["graphics.ini"].GetInt("graph1", "Key_X") == 385, "Unit5 registry graph field mapping");
            var empty = EngineImport.ConvertProfile(new LegacyEngineProfile { Name = "empty" }, 20);
            Check(empty["jobs.ini"].Get("configs", "ParameterNumbers2") == "" && empty["graphics.ini"].GetInt("graphics", "count") == 0, "Unit5 missing lists do not reuse stale values");
            profile.Jobs[1] = "bad"; ExpectFailure(() => EngineImport.ConvertProfile(profile, 19), "Unit5 malformed job rejected"); profile.Jobs[1] = "calc.exe/51 1210/0 2";
            ExpectFailure(() => profile.SourcePath(@"C:\Models", @"..\..\outside"), "Unit5 registry path traversal rejected");
            string root = Path.Combine(Path.GetTempPath(), "Unit5Checks-" + Guid.NewGuid());
            try
            {
                Directory.CreateDirectory(Path.Combine(root, "Target", "Ru"));
                string target = Path.Combine(root, "Target"), sources = Path.Combine(root, "Sources");
                File.WriteAllText(Path.Combine(target, "Ru", "jobs.ini"), "[Engine]\r\ncount=0\r\n");
                Directory.CreateDirectory(profile.SourcePath(sources, "datar")); Directory.CreateDirectory(profile.SourcePath(sources, "result"));
                File.WriteAllText(profile.SourcePath(sources, "datar\\project.inm"), "project"); File.WriteAllText(profile.SourcePath(sources, "result\\result.res"), "result");
                byte[] picture;
                using (var bitmap = new Bitmap(2, 2)) using (var stream = new MemoryStream()) { bitmap.Save(stream, ImageFormat.Bmp); picture = stream.ToArray(); }
                var request = new EngineCreationRequest { Name = "renamed-engine", Code = 19, Description = "Импорт", PictureBmp = picture };
                string created = EngineImport.Import(target, "Ru", profile, request, sources);
                Check(File.ReadAllText(Path.Combine(created, "datar", "project.inm")) == "project" && File.ReadAllText(Path.Combine(created, "result", "result.res")) == "result", "Unit5 renamed destination copies original source projects/results");
                Check(IniFile.Load(Path.Combine(created, "Ru", "jobs.ini")).GetInt("jobs", "n2") == 7 && IniFile.Load(Path.Combine(created, "Ru", "graphics.ini")).GetInt("graph1", "n") == 777, "Unit5 imported settings replace creation defaults");
                Check(IniFile.Load(Path.Combine(target, "Ru", "jobs.ini")).Get("Engine", "n1") == "renamed-engine", "Unit5 import registers final name");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
        private static void ExpectFailure(Action action, string label)
        { try { action(); } catch (InvalidDataException) { Console.WriteLine("PASS " + label); return; } throw new Exception(label); }
        private static void Check(bool condition, string label) { if (!condition) throw new Exception(label); Console.WriteLine("PASS " + label); }
    }
}
