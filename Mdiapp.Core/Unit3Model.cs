using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mdiapp.Core
{
    public sealed class Unit3Parameter
    {
        public int Number { get; set; }
        public string Symbol { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string Node { get; set; }
        public string Name { get; set; }
    }
    public sealed class Unit3Choice
    {
        public bool Enabled { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
    }
    public sealed class Unit3Job
    {
        public bool Enabled { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public string Executable { get; set; }
        public string Preg { get; set; }
    }
    public sealed class Unit3Include
    {
        public int Number { get; set; }
        public int Graphic { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
    }
    public sealed class Unit3Graph
    {
        public int n { get; set; }
        public int DiffPointCount { get; set; } = 1;
        public int Key_bottom_Y { get; set; } = 1;
        public int Key_top_Y { get; set; } = 1;
        public int Key_X { get; set; } = 1;
        public int LookTogether { get; set; }
        public int Par { get; set; } = 1;
        public int ParKey { get; set; } = 1;
        public int StandartFile_bottom_Y { get; set; } = 1;
        public int StandartFile_top_Y { get; set; } = 1;
        public int StandartFile_X { get; set; } = 1;
        public int WorkFile_bottom_Y { get; set; } = 1;
        public int WorkFile_top_Y { get; set; } = 1;
        public int WorkFile_X { get; set; } = 1;
    }
    public sealed class Unit3OrderedParameter
    {
        public int Number { get; set; }
        public bool Enabled { get; set; }
    }
    public sealed class Unit3Engine
    {
        public int Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    public sealed class Unit3Setting
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
    }

    // State behind Unit3. Lists use original IDs; presentation row numbers are never persisted as IDs.
    public sealed class Unit3Model
    {
        public string Root { get; }
        public string Language { get; }
        public string Engine { get; }
        public IniFile GlobalJobs { get; }
        public IniFile EngineJobs { get; }
        public IniFile InputFiles { get; }
        public IniFile Graphics { get; }
        public IniFile Dynamics { get; }
        public IniFile MiscFile { get; }
        public IniFile Models { get; }
        public List<Unit3Engine> Engines { get; } = new List<Unit3Engine>();
        public List<Unit3Choice> Configurations { get; } = new List<Unit3Choice>();
        public List<Unit3Choice> Conditions { get; } = new List<Unit3Choice>();
        public List<Unit3Choice> Misc { get; } = new List<Unit3Choice>();
        public List<Unit3Job> Jobs { get; } = new List<Unit3Job>();
        public List<Unit3Parameter> Parameters { get; }
        public List<Unit3Parameter> Characteristics { get; }
        public List<Unit3Include> Includes { get; }
        public List<int> IncludeOrder { get; }
        public HashSet<int> SelectedParameters { get; }
        public HashSet<int> SelectedControls { get; }
        public HashSet<int> SelectedCharacteristics { get; }
        public HashSet<int> SelectedFile30 { get; }
        public List<Unit3Graph> Graphs { get; } = new List<Unit3Graph>();
        public List<Unit3OrderedParameter> File3132 { get; } = new List<Unit3OrderedParameter>();
        public Dictionary<int, List<string>> DynamicFactors { get; } = new Dictionary<int, List<string>>();
        public int TimeParameter { get; set; }
        private readonly string includeTrailer;
        public Unit3Model(string root, string language, string engine)
        {
            Root = Path.GetFullPath(root); Language = SafeName(language); Engine = SafeName(engine);
            GlobalJobs = IniFile.Load(GlobalPath("jobs.ini")); EngineJobs = IniFile.Load(EnginePath("jobs.ini"));
            InputFiles = IniFile.Load(EnginePath("inputfiles.ini")); Graphics = IniFile.Load(EnginePath("graphics.ini"));
            Dynamics = IniFile.Load(EnginePath("dinamfact.ini")); MiscFile = IniFile.Load(EnginePath("misc.ini"));
            Models = IniFile.Load(Path.Combine(Root, "models.ini"));
            Parameters = ReadParameters(GlobalPath("Namepar.txt")); Characteristics = ReadParameters(GlobalPath("Namehar.txt"));
            if (File.Exists(EnginePath("Namepar1.txt")))
            foreach (var parameter in ReadParameters(EnginePath("Namepar1.txt")))
            {
                int index = Parameters.FindIndex(p => p.Number == parameter.Number);
                if (index >= 0) Parameters[index] = parameter; else Parameters.Add(parameter);
            }
            Parameters.Sort((a, b) => a.Number.CompareTo(b.Number));
            string includePath = GlobalPath("includes.ini");
            Includes = ReadIncludes(includePath);
            var text = File.Exists(includePath) ? File.ReadAllText(includePath, LegacyCodec.TextEncoding) : "";
            var marker = Regex.Match(text, @"(?m)^\s*-1[^\r\n]*(?:\r?\n)?");
            includeTrailer = marker.Success ? text.Substring(marker.Index + marker.Length) : "";
            for (int i = 1; i <= Count(GlobalJobs, "Engine", "count", 100); i++)
                Engines.Add(new Unit3Engine { Code = GlobalJobs.GetInt("Engine", "nn" + i), Name = GlobalJobs.Get("Engine", "n" + i), Description = GlobalJobs.Get("Engine", "ndescript" + i) });
            var configs = ParseNumbers(EngineJobs.Get("configs", "configs"), 0, 30);
            for (int i = 0; i < Count(GlobalJobs, "configs", "count", 31); i++)
                Configurations.Add(new Unit3Choice { Number = i, Name = GlobalJobs.Get("configs", "n" + i), Enabled = configs.Contains(i) });
            var conditions = ParseNumbers(EngineJobs.Get("configs", "Conditions"), 0, 30);
            for (int i = 0; i < Count(GlobalJobs, "Conditions", "count", 31); i++)
                Conditions.Add(new Unit3Choice { Number = i, Name = GlobalJobs.Get("Conditions", "n" + i), Enabled = conditions.Contains(i) });
            var misc = ParseNumbers(EngineJobs.Get("misc", "misc"), 0, 30);
            for (int i = 0; i < Count(MiscFile, "misc", "count", 31); i++)
            {
                int id = MiscFile.GetInt("misc", "nn" + i);
                Misc.Add(new Unit3Choice { Number = id, Name = MiscFile.Get("misc", "n" + i), Enabled = misc.Contains(id) });
            }
            for (int i = 1; i <= Count(GlobalJobs, "jobs", "jobs_count", 30); i++)
            {
                int slot = Enumerable.Range(1, Count(EngineJobs, "jobs", "n_count", 30)).FirstOrDefault(n => EngineJobs.GetInt("jobs", "n" + n) == i);
                Jobs.Add(new Unit3Job { Number = i, Name = GlobalJobs.Get("jobs", "n" + i), Enabled = slot > 0,
                    Executable = slot == 0 ? "" : EngineJobs.Get("jobs", "p" + slot), Preg = slot == 0 ? "" : EngineJobs.Get("jobs", "preg" + slot) });
            }
            IncludeOrder = ParseNumbers(EngineJobs.Get("jobs", "f1"), 1, 2000);
            SelectedParameters = new HashSet<int>(ReadPair(EngineJobs, "ParameterNumbers"));
            SelectedControls = new HashSet<int>(ReadPair(EngineJobs, "ParUprNumbers"));
            SelectedCharacteristics = new HashSet<int>(ParseNumbers(EngineJobs.Get("configs", "CharacteristicNumbers"), 1, 2000));
            SelectedFile30 = new HashSet<int>(ParseNumbers(InputFiles.Get("30", "parametry"), 1, 2000));
            TimeParameter = EngineJobs.GetInt("configs", "time", 385);
            for (int i = 1; i <= Count(Graphics, "graphics", "count", 200); i++)
            {
                var graph = new Unit3Graph();
                foreach (var property in typeof(Unit3Graph).GetProperties())
                    property.SetValue(graph, Graphics.GetInt("graph" + i, property.Name, (int)property.GetValue(graph)));
                Graphs.Add(graph);
            }
            for (int i = 1; i <= Count(InputFiles, "31_32", "count", 100); i++)
                File3132.Add(new Unit3OrderedParameter { Number = InputFiles.GetInt("31_32", "n" + i), Enabled = InputFiles.GetInt("31_32", "e" + i) == 1 });
            for (int i = 0; i <= 30; i++)
                DynamicFactors[i] = Enumerable.Range(1, Count(Dynamics, "dinam" + i, "count", 30)).Select(n => Dynamics.Get("dinam" + i, "n" + n)).ToList();
        }
        public string GlobalPath(string file) => Path.Combine(Root, Language, file);
        public string EnginePath(string file) => Path.Combine(Root, Engine, Language, file);
        private static string SafeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidDataException("Недопустимое имя каталога.");
            return name;
        }
        private static int Count(IniFile ini, string section, string key, int maximum)
        {
            int count = ini.GetInt(section, key);
            if (count < 0 || count > maximum) throw new InvalidDataException(section + ": недопустимое количество записей.");
            return count;
        }
        public static List<int> ParseNumbers(string value, int minimum, int maximum)
        {
            var result = new List<int>();
            foreach (var word in value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(word, out int number) || number < minimum || number > maximum) throw new InvalidDataException("Недопустимый номер: " + word);
                result.Add(number);
            }
            return result;
        }
        private static IEnumerable<int> ReadPair(IniFile ini, string key) => ParseNumbers(ini.Get("configs", key + "1") + " " + ini.Get("configs", key + "2"), 1, 2000);
        public static List<Unit3Parameter> ReadParameters(string path)
        {
            var result = new List<Unit3Parameter>();
            if (!File.Exists(path)) throw new FileNotFoundException("Отсутствует справочник параметров", path);
            foreach (string line in File.ReadLines(path, LegacyCodec.TextEncoding))
            {
                string[] fields = Regex.Split(line.Trim(), @"\s+", RegexOptions.None, TimeSpan.FromSeconds(1));
                if (fields[0] == "-1") break;
                if (fields.Length < 6 || !int.TryParse(fields[0], out int number) || number < 1 || number > 2000) throw new InvalidDataException("Неверная запись справочника: " + line);
                if (result.Any(p => p.Number == number)) throw new InvalidDataException("Повтор номера в справочнике.");
                result.Add(new Unit3Parameter { Number = number, Symbol = fields[1], Type = fields[2], Value = fields[3], Node = fields[4], Name = string.Join(" ", fields.Skip(5)) });
            }
            return result;
        }
        public static List<Unit3Include> ReadIncludes(string path)
        {
            var result = new List<Unit3Include>();
            if (!File.Exists(path)) return result;
            foreach (string line in File.ReadLines(path, LegacyCodec.TextEncoding))
            {
                var match = Regex.Match(line, @"^\s*(-?\d+)\s+(\d+)\s+(\S+)\s+(.*)$");
                if (line.Trim() == "-1") break;
                if (!match.Success) throw new InvalidDataException("Неверная запись типа файла: " + line);
                result.Add(new Unit3Include { Number = int.Parse(match.Groups[1].Value), Graphic = int.Parse(match.Groups[2].Value), Symbol = match.Groups[3].Value, Name = match.Groups[4].Value });
            }
            return result;
        }
        public static void WritePair(IniFile ini, string key, IEnumerable<int> numbers)
        {
            var first = new List<string>(); var second = new List<string>(); int length = 0;
            foreach (int number in numbers)
            {
                string value = number.ToString(CultureInfo.InvariantCulture);
                if (length < 1900) { first.Add(value); length += value.Length + 1; } else second.Add(value);
            }
            // The original moves the last item to the second key when all IDs fit in the first key.
            if (second.Count == 0 && first.Count > 1) { second.Add(first[first.Count - 1]); first.RemoveAt(first.Count - 1); }
            ini.Set("configs", key + "1", string.Join(" ", first)); ini.Set("configs", key + "2", string.Join(" ", second));
        }
        private static void ValidateIds(IEnumerable<int> ids, int min, int max, int capacity)
        {
            int[] list = ids.ToArray();
            if (list.Length > capacity || list.Distinct().Count() != list.Length || list.Any(id => id < min || id > max)) throw new InvalidDataException("Повторяющиеся номера или превышен диапазон записей.");
        }
        public void Save()
        {
            ValidateIds(Engines.Select(e => e.Code), 1, 100, 100); foreach (var e in Engines) SafeName(e.Name);
            ValidateIds(Configurations.Select(c => c.Number), 0, 30, 31); ValidateIds(Jobs.Select(j => j.Number), 1, 30, 30);
            ValidateIds(Misc.Select(m => m.Number), 0, 30, 31); ValidateIds(Includes.Select(f => f.Number), 1, 2000, 2000);
            ValidateIds(IncludeOrder, 1, 2000, 200);
            if (TimeParameter < 1 || TimeParameter > 2000 || Graphs.Count > 200 || File3132.Count > 100) throw new InvalidDataException("Превышен диапазон настроек.");
            if (Includes.Any(f => string.IsNullOrWhiteSpace(f.Symbol) || f.Symbol.Any(char.IsWhiteSpace) || f.Name == null || f.Name.IndexOfAny(new[] {'\r', '\n'}) >= 0)) throw new InvalidDataException("Укажите символ без пробелов и название типа файла в одну строку.");
            if (File3132.Any(p => p.Number < 0 || p.Number > 2000)) throw new InvalidDataException("Недопустимый параметр 31–32.");
            foreach (string section in new[] {"24", "28", "41", "51", "2940", "31_32", "rez", "paramdat", "preg"})
            foreach (var entry in InputFiles.Entries(section))
            {
                bool boolean = entry.Key.StartsWith("ne", StringComparison.OrdinalIgnoreCase) || entry.Key.StartsWith("ce", StringComparison.OrdinalIgnoreCase)
                    || entry.Key.StartsWith("re", StringComparison.OrdinalIgnoreCase) || section == "2940" && entry.Key.StartsWith("n") || section == "rez" || section == "31_32" && entry.Key.StartsWith("preg");
                if (boolean && entry.Value != "0" && entry.Value != "1" && !bool.TryParse(entry.Value, out _)) throw new InvalidDataException(section + "/" + entry.Key + ": укажите 0 или 1.");
                bool integer = section == "paramdat" || section == "preg" || section == "28" && (entry.Key.StartsWith("nrow") || entry.Key == "n28count");
                if (integer && !int.TryParse(entry.Value, out _)) throw new InvalidDataException(section + "/" + entry.Key + ": укажите целое число.");
            }
            ValidateIds(SelectedParameters, 1, 2000, 2000); ValidateIds(SelectedControls, 1, 2000, 2000);
            ValidateIds(SelectedCharacteristics, 1, 2000, 2000); ValidateIds(SelectedFile30, 1, 2000, 2000);
            foreach (var ini in new[] {GlobalJobs, EngineJobs, InputFiles, Graphics, Dynamics, MiscFile, Models})
            foreach (string section in ini.SectionNames)
            foreach (var entry in ini.Entries(section))
            {
                if (entry.Value.IndexOfAny(new[] {'\r', '\n'}) >= 0) throw new InvalidDataException("Значения INI должны занимать одну строку.");
                if (ReferenceEquals(ini, InputFiles) && entry.Key.Equals("new", StringComparison.OrdinalIgnoreCase) && entry.Value != "0" && entry.Value != "1" && !bool.TryParse(entry.Value, out _))
                    throw new InvalidDataException(section + "/new: укажите 0 или 1.");
            }
            for (int i = 0; i < Engines.Count; i++) { var e = Engines[i]; GlobalJobs.Set("Engine", "nn" + (i + 1), e.Code.ToString()); GlobalJobs.Set("Engine", "n" + (i + 1), e.Name); GlobalJobs.Set("Engine", "ndescript" + (i + 1), e.Description ?? ""); }
            GlobalJobs.Set("Engine", "count", Engines.Count.ToString());
            SaveChoices(Configurations, GlobalJobs, "configs", "count", EngineJobs, "configs", "configs", 0);
            // Keep stable configuration IDs. Delphi's list is dense and cannot express holes.
            if (Configurations.OrderBy(c => c.Number).Where((c, i) => c.Number != i).Any()) throw new InvalidDataException("Конфигурации должны иметь последовательные номера от 0.");
            if (Jobs.OrderBy(j => j.Number).Where((j, i) => j.Number != i + 1).Any()) throw new InvalidDataException("Задачи должны иметь последовательные номера от 1.");
            GlobalJobs.Set("jobs", "jobs_count", Jobs.Count.ToString());
            int slot = 0;
            foreach (var job in Jobs.OrderBy(j => j.Number))
            {
                GlobalJobs.Set("jobs", "n" + job.Number, job.Name ?? "");
                ParseNumbers(job.Preg ?? "", 0, 3);
                if (!job.Enabled) continue;
                slot++; EngineJobs.Set("jobs", "n" + slot, job.Number.ToString()); EngineJobs.Set("jobs", "p" + slot, job.Executable ?? "");
                EngineJobs.Set("jobs", "preg" + slot, job.Preg ?? ""); EngineJobs.Set("jobs", "f" + slot, string.Join(" ", IncludeOrder));
            }
            EngineJobs.Set("jobs", "n_count", slot.ToString()); EngineJobs.Set("configs", "time", TimeParameter.ToString());
            EngineJobs.Set("configs", "Conditions", string.Join(" ", Conditions.Where(c => c.Enabled).Select(c => c.Number)));
            SaveChoices(Misc, MiscFile, "misc", "count", EngineJobs, "misc", "misc", 0, true);
            WritePair(EngineJobs, "ParameterNumbers", new[] {TimeParameter}.Concat(SelectedParameters.Where(n => n != TimeParameter).OrderBy(n => n)));
            WritePair(EngineJobs, "ParUprNumbers", SelectedControls.OrderBy(n => n));
            EngineJobs.Set("configs", "CharacteristicNumbers", string.Join(" ", SelectedCharacteristics.OrderBy(n => n)));
            InputFiles.Set("30", "parametry", string.Join(" ", SelectedFile30.OrderBy(n => n)));
            for (int i = 0; i < File3132.Count; i++) { InputFiles.Set("31_32", "n" + (i + 1), File3132[i].Number.ToString()); InputFiles.Set("31_32", "e" + (i + 1), i < 6 && File3132[i].Enabled ? "1" : "0"); }
            InputFiles.Set("31_32", "count", File3132.Count.ToString());
            for (int i = 0; i < Graphs.Count; i++) foreach (var property in typeof(Unit3Graph).GetProperties()) Graphics.Set("graph" + (i + 1), property.Name, property.GetValue(Graphs[i]).ToString());
            Graphics.Set("graphics", "count", Graphs.Count.ToString());
            foreach (var pair in DynamicFactors)
            {
                if (pair.Value.Count > 30) throw new InvalidDataException("Максимум 30 динамических факторов.");
                if (pair.Value.Any(value => value == null || value.IndexOfAny(new[] {'\r', '\n'}) >= 0)) throw new InvalidDataException("Название динамического фактора должно занимать одну строку.");
                Dynamics.Set("dinam" + pair.Key, "count", pair.Value.Count.ToString());
                for (int i = 0; i < pair.Value.Count; i++) { Dynamics.Set("dinam" + pair.Key, "n" + (i + 1), pair.Value[i]); Dynamics.Set("dinam" + pair.Key, "nb" + (i + 1), "0"); }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(EnginePath("jobs.ini")));
            var outputs = new Dictionary<string, IniFile> { [GlobalPath("jobs.ini")] = GlobalJobs, [EnginePath("jobs.ini")] = EngineJobs,
                [EnginePath("inputfiles.ini")] = InputFiles, [EnginePath("graphics.ini")] = Graphics, [EnginePath("dinamfact.ini")] = Dynamics, [EnginePath("misc.ini")] = MiscFile, [Path.Combine(Root, "models.ini")] = Models };
            var staged = new Dictionary<string, string>();
            try
            {
                foreach (var output in outputs) { string temp = output.Key + ".new-" + Guid.NewGuid().ToString("N"); staged.Add(output.Key, temp); output.Value.Save(temp); }
                string includeTemp = GlobalPath("includes.ini") + ".new-" + Guid.NewGuid().ToString("N"); staged.Add(GlobalPath("includes.ini"), includeTemp);
                File.WriteAllText(includeTemp, string.Join("\r\n", Includes.OrderBy(f => f.Number).Select(f => $"{f.Number} {f.Graphic} {f.Symbol} {f.Name}")) + "\r\n-1\r\n" + includeTrailer, LegacyCodec.TextEncoding);
                foreach (var output in staged)
                {
                    if (File.Exists(output.Key)) File.Replace(output.Value, output.Key, output.Key + ".bak"); else File.Move(output.Value, output.Key);
                }
                foreach (string path in new[] {GlobalPath("jobs.ini"), EnginePath("jobs.ini")})
                { using var input = File.OpenRead(path); using var coded = File.Create(path + ".cod"); LegacyCodec.Encode(input, coded); }
            }
            finally { foreach (string temp in staged.Values) if (File.Exists(temp)) File.Delete(temp); }
        }
        private static void SaveChoices(List<Unit3Choice> choices, IniFile names, string section, string countKey, IniFile selected, string selectedSection, string selectedKey, int start, bool sparse = false)
        {
            var ordered = choices.OrderBy(c => c.Number).ToList();
            for (int i = 0; i < ordered.Count; i++) { names.Set(section, "n" + (i + start), ordered[i].Name ?? ""); if (sparse) names.Set(section, "nn" + (i + start), ordered[i].Number.ToString()); }
            names.Set(section, countKey, ordered.Count.ToString()); selected.Set(selectedSection, selectedKey, string.Join(" ", ordered.Where(c => c.Enabled).Select(c => c.Number)));
        }
    }
}
