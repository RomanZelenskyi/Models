using System;
using System.IO;

namespace Mdiapp.Core
{
    public sealed class ResultExportOption
    {
        public bool Transpose { get; set; }
        public bool IncludeInput { get; set; }
        public bool IncludeGraphs { get; set; }
        public int FormatIndex { get; set; }
        public int Digits { get; set; }
    }
    public sealed class ResultExportSettings
    {
        public ResultExportOption[] Formats { get; } = { new ResultExportOption(), new ResultExportOption(), new ResultExportOption() };
        public static ResultExportSettings Load(string path)
        {
            var ini = IniFile.Load(path); var settings = new ResultExportSettings();
            for (int i = 0; i < 3; i++)
            {
                string n = (i + 1).ToString(); var option = settings.Formats[i];
                option.Transpose = ReadBool(ini.Get("ressave", "transp" + n)); option.IncludeInput = ReadBool(ini.Get("ressave", "ish" + n));
                option.IncludeGraphs = ReadBool(ini.Get("ressave", "graph" + n)); option.FormatIndex = ini.GetInt("ressave", "formatindex" + n); option.Digits = ini.GetInt("ressave", "digs" + n);
            }
            return settings;
        }
        private static bool ReadBool(string value) => value == "1" || bool.TryParse(value, out bool parsed) && parsed;
        public void Save(string path)
        {
            var ini = IniFile.Load(path);
            for (int i = 0; i < 3; i++)
            {
                string n = (i + 1).ToString(); var option = Formats[i];
                ini.Set("ressave", "transp" + n, option.Transpose ? "1" : "0"); ini.Set("ressave", "ish" + n, option.IncludeInput ? "1" : "0");
                ini.Set("ressave", "graph" + n, option.IncludeGraphs ? "1" : "0"); ini.Set("ressave", "formatindex" + n, option.FormatIndex.ToString()); ini.Set("ressave", "digs" + n, option.Digits.ToString());
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temporary = path + ".new-" + Guid.NewGuid().ToString("N");
            try { ini.Save(temporary); if (File.Exists(path)) File.Replace(temporary, path, path + ".bak"); else File.Move(temporary, path); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
