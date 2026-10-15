using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace Mdiapp.Core
{
    public sealed class LegacyEngineProfile
    {
        public string Name { get; set; }
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<int, string> Jobs { get; } = new Dictionary<int, string>();
        public List<int> Configurations { get; } = new List<int>();
        public Dictionary<int, string> Misc { get; } = new Dictionary<int, string>();
        public Dictionary<int, Dictionary<string, int>> Graphs { get; } = new Dictionary<int, Dictionary<string, int>>();
        public string Get(string name, string fallback = "") => Values.TryGetValue(name, out string value) ? value : fallback;
        public override string ToString() => Name;
        public string SourcePath(string projectRoot, string relative)
        {
            if (string.IsNullOrWhiteSpace(relative)) return "";
            if (string.IsNullOrWhiteSpace(Name) || Name == "." || Name == ".." || Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new InvalidDataException("Недопустимое имя исходного проекта.");
            string folder = Path.GetFullPath(Path.Combine(projectRoot, Name));
            string path = Path.GetFullPath(Path.Combine(folder, relative));
            if (!path.StartsWith(folder.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Путь в настройках выходит за папку импортируемого проекта.");
            return path;
        }
    }
    public static class LegacyEngineRegistry
    {
        private const string BaseKey = @"Software\KhAI (AEDLab)\Models";
        public static IReadOnlyList<string> ListProjects()
        {
            using var user = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            using var settings = user.OpenSubKey(BaseKey + @"\Settings", false);
            var names = Convert.ToString(settings?.GetValue("ProjectList", ""));
            return ParseProjectList(names);
        }
        public static IReadOnlyList<string> ParseProjectList(string value)
        {
            string[] names = (value ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (names.Length > 100) throw new InvalidDataException("Список содержит более 100 проектов.");
            return names;
        }
        public static LegacyEngineProfile Read(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name == "." || name == "..") throw new InvalidDataException("Недопустимое имя проекта реестра.");
            using var user = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            using var project = user.OpenSubKey(BaseKey + "\\" + name, false);
            if (project == null) throw new InvalidDataException("Проект отсутствует в реестре: " + name);
            var result = new LegacyEngineProfile { Name = name };
            foreach (string key in new[] {"Key", "Project description", "DopParFileName", "BmpFileName", "Executable files directiory", "Characteristic files", "Accuracies", "Input conditions", "Parameter numbers", "Characteristic numbers"})
                result.Values[key] = Convert.ToString(project.GetValue(key, ""), CultureInfo.InvariantCulture);
            using (var jobs = project.OpenSubKey("Jobs", false))
            using (var configs = project.OpenSubKey("Configurations", false))
            using (var misc = project.OpenSubKey("Miscellaneous", false))
            {
                for (int i = 0; i <= 30; i++)
                {
                    string key = i.ToString();
                    if (jobs?.GetValue(key) != null) result.Jobs[i] = Convert.ToString(jobs.GetValue(key));
                    if (configs?.GetValue(key) != null) result.Configurations.Add(i);
                    if (misc?.GetValue(key) != null) result.Misc[i] = Convert.ToString(misc.GetValue(key));
                }
            }
            using var lines = project.OpenSubKey("Regim Lines", false);
            if (lines != null)
            foreach (string subkey in lines.GetSubKeyNames())
            {
                if (!int.TryParse(subkey, out int number) || number < 1 || number > 2000) continue;
                using var line = lines.OpenSubKey(subkey, false);
                var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (string key in EngineImport.GraphKeys.Values)
                {
                    object value = line?.GetValue(key);
                    if (value == null || !int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out int integer)) throw new InvalidDataException("Не найдено целочисленное поле графика: " + subkey + "/" + key);
                    values[key] = integer;
                }
                result.Graphs[number] = values;
            }
            return result;
        }
    }
    public static class EngineImport
    {
        public static readonly IReadOnlyDictionary<string, string> GraphKeys = new Dictionary<string, string>
        {
            ["DiffPointCount"] = "DiffPointCount", ["Key_bottom_Y"] = "Key (bottom Y)", ["Key_top_Y"] = "Key (top Y)", ["Key_X"] = "Key (X)",
            ["LookTogether"] = "LookTogether", ["Par"] = "Par4", ["ParKey"] = "ParKey", ["StandartFile_bottom_Y"] = "StandartFile (bottom Y)",
            ["StandartFile_top_Y"] = "StandartFile (top Y)", ["StandartFile_X"] = "StandartFile (X)", ["WorkFile_bottom_Y"] = "WorkFile (bottom Y)",
            ["WorkFile_top_Y"] = "WorkFile (top Y)", ["WorkFile_X"] = "WorkFile (X)"
        };
        public static Dictionary<string, IniFile> ConvertProfile(LegacyEngineProfile profile, int code, CultureInfo culture = null)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (profile.Graphs.Count > 200) throw new InvalidDataException("Текущая модель поддерживает не более 200 графиков.");
            culture = culture ?? CultureInfo.CurrentCulture;
            var jobs = new IniFile(); int slot = 0;
            foreach (var pair in profile.Jobs.OrderBy(p => p.Key))
            {
                if (pair.Key < 0 || pair.Key > 30) throw new InvalidDataException("Недопустимый номер задачи.");
                string[] parts = pair.Value.Split(new[] {'/'}, 3);
                if (parts.Length != 3) throw new InvalidDataException("Задача " + pair.Key + ": требуется формат EXE/типы файлов/PREG.");
                Unit3Model.ParseNumbers(parts[1], 1, 2000); Unit3Model.ParseNumbers(parts[2], 0, 3);
                slot++; jobs.Set("jobs", "n" + slot, pair.Key.ToString()); jobs.Set("jobs", "p" + slot, parts[0]); jobs.Set("jobs", "f" + slot, parts[1]); jobs.Set("jobs", "preg" + slot, parts[2]);
            }
            jobs.Set("jobs", "n_count", slot.ToString()); jobs.Set("configs", "key", code.ToString());
            jobs.Set("configs", "configs", string.Join(" ", profile.Configurations.OrderBy(n => n)));
            jobs.Set("misc", "misc", string.Join(" ", profile.Misc.Keys.OrderBy(n => n))); jobs.Set("misc", "n_count", "0");
            jobs.Set("configs", "tochnost", profile.Get("Accuracies"));
            // Unit5 initializes the condition to 0, irrespective of the unused registry Input conditions value.
            jobs.Set("configs", "Conditions", "0"); jobs.Set("configs", "time", "385");
            jobs.Set("configs", "t1", 0.00001.ToString("G", culture)); jobs.Set("configs", "t2", 0.000001.ToString("G", culture)); jobs.Set("configs", "t3", 0.1.ToString("G", culture));
            var parameters = Unit3Model.ParseNumbers(profile.Get("Parameter numbers"), 1, 2000);
            jobs.Set("configs", "ParameterNumbers1", string.Join(" ", parameters.Take(Math.Max(0, parameters.Count - 1)))); jobs.Set("configs", "ParameterNumbers2", parameters.Count == 0 ? "" : parameters.Last().ToString());
            Unit3Model.ParseNumbers(profile.Get("Characteristic numbers"), 1, 2000); jobs.Set("configs", "CharacteristicNumbers", profile.Get("Characteristic numbers"));
            var misc = new IniFile(); int index = 0;
            foreach (var pair in profile.Misc.OrderBy(p => p.Key)) { if (pair.Key < 0 || pair.Key > 30) throw new InvalidDataException("Недопустимый номер пункта Разное."); misc.Set("misc", "n" + index, pair.Value); misc.Set("misc", "nn" + index, pair.Key.ToString()); index++; }
            misc.Set("misc", "count", index.ToString());
            var graphics = new IniFile(); index = 0;
            foreach (var pair in profile.Graphs.OrderBy(p => p.Key))
            {
                if (pair.Key < 1 || pair.Key > 2000) throw new InvalidDataException("Недопустимый номер графика.");
                string section = "graph" + ++index; graphics.Set(section, "n", pair.Key.ToString());
                foreach (var key in GraphKeys) { if (!pair.Value.TryGetValue(key.Value, out int value)) throw new InvalidDataException("Отсутствует поле графика " + key.Value); graphics.Set(section, key.Key, value.ToString()); }
            }
            graphics.Set("graphics", "count", index.ToString());
            var result = new Dictionary<string, IniFile> { ["jobs.ini"] = jobs, ["misc.ini"] = misc, ["graphics.ini"] = graphics };
            foreach (var ini in result.Values) foreach (string section in ini.SectionNames) foreach (var entry in ini.Entries(section))
                if (entry.Value.IndexOfAny(new[] {'\r', '\n'}) >= 0) throw new InvalidDataException("Настройка реестра должна занимать одну строку.");
            Unit3Model.ParseNumbers(string.Join(" ", profile.Configurations), 0, 30);
            return result;
        }
        public static string Import(string root, string language, LegacyEngineProfile profile, EngineCreationRequest request, string sourceProjectsRoot, IProgress<int> progress = null)
        {
            request.ImportedSettings = ConvertProfile(profile, request.Code);
            string projects = profile.SourcePath(sourceProjectsRoot, "datar"), results = profile.SourcePath(sourceProjectsRoot, "result");
            request.ProjectsDirectory = Directory.Exists(projects) ? projects : null; request.ResultsDirectory = Directory.Exists(results) ? results : null;
            return EngineCreation.Create(root, language, request, progress);
        }
    }
}
