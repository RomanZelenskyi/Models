using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    public sealed class LanguageOption
    {
        public string Name { get; }
        public string Code { get; }
        public string HelpFile { get; }
        public LanguageOption(string name, string code, string helpFile) { Name = name; Code = code; HelpFile = helpFile; }
        public override string ToString() => Name;
    }
    public sealed class EngineOption
    {
        public string Name { get; }
        public string Description { get; }
        public int Code { get; }
        public EngineOption(string name, string description, int code) { Name = name; Description = description; Code = code; }
        public override string ToString() => Name;
    }

    // Unit7 and Unit10: keep the sparse engine codes from jobs.ini.
    public sealed class ApplicationSettings
    {
        public string DataDirectory { get; }
        public string PreferencesPath => settingsPath;
        public IReadOnlyCollection<int> ActiveParameterIds { get; set; }
        public IReadOnlyDictionary<int,double> NodeCorrections { get; set; }
        public bool IsParameterActive(int number)=>ActiveParameterIds==null||ActiveParameterIds.Contains(number);
        public double NodeCorrection(int number)=>NodeCorrections!=null&&NodeCorrections.TryGetValue(number,out double value)?value:0;
        public double[] CorrectionPreset(int type)
        {
            int[] ids;
            switch(type){case 1:ids=new[]{1,2,3};break;case 11:ids=new[]{84,0,85};break;case 16:ids=new[]{86,0,87};break;case 20:ids=new[]{90,0,91};break;case 14:ids=Enumerable.Range(64,6).ToArray();break;case 21:ids=new[]{92,0,93,94,0,95,96,97,98};break;case 22:ids=new[]{99,0,100,101,0,102,103,104,105};break;case 3:ids=new[]{11,12,13,15,16,17};break;case 4:ids=new[]{18,19,20,22,23,24};break;case 15:ids=Enumerable.Range(46,6).ToArray();break;case 17:ids=Enumerable.Range(52,6).ToArray();break;case 19:ids=Enumerable.Range(58,6).ToArray();break;default:ids=new[]{39,40,41,43,44,45};break;}return ids.Select(NodeCorrection).ToArray();
        }
        public double XCorrectionPreset(int type)=>NodeCorrection(type==3?14:type==4?21:42);
        private readonly string settingsPath;
        private readonly IniFile settings;
        public IReadOnlyList<LanguageOption> Languages { get; }
        public string Language => settings.Get("Interface", "lang", "Ru");
        public string Engine => settings.Get("Engine", "name");
        public int EngineCode => settings.GetInt("Engine", "code", 1);
        public string Version => IniFile.Load(Path.Combine(DataDirectory, "models.ini")).Get("Interface", "version", settings.Get("Interface", "version"));
        public IniFile Menu => IniFile.Load(Path.Combine(DataDirectory, Language, "menu.ini"));
        public string PreferredLanguage => ReadPreferences().Get("Interface", "lang", Language);
        public bool ShowUpdateNotice => ReadPreferences().GetInt("Interface","newshow",1)==1;
        public string UpdateNoticePath
        {
            get
            {
                string name=IniFile.Load(Path.Combine(DataDirectory,"models.ini")).Get("Interface","new",settings.Get("Interface","new"));
                if(string.IsNullOrWhiteSpace(name))return null;
                string root=Path.GetFullPath(Path.Combine(DataDirectory,Language))+Path.DirectorySeparatorChar;
                string path=Path.GetFullPath(Path.Combine(root,name));if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Файл новостей должен находиться в папке языка.");return path;
            }
        }
        public void SetShowUpdateNotice(bool show)
        {
            var preferences=ReadPreferences();preferences.Set("Interface","newshow",show?"1":"0");SavePreferences(preferences);
        }
        private IniFile ReadPreferences() => IniFile.Load(File.Exists(settingsPath) ? settingsPath : Path.Combine(DataDirectory, "models.ini"));
        public bool IsLanguageAvailable(LanguageOption language) => language != null && Languages.Any(l => l.Code == language.Code)
            && File.Exists(Path.Combine(DataDirectory, language.Code, "menu.ini"));
        public bool ChangeLanguage(LanguageOption language)
        {
            if (language == null) throw new ArgumentNullException(nameof(language));
            var selected = Languages.FirstOrDefault(l => l.Code == language.Code);
            if (selected == null) throw new InvalidOperationException("Язык отсутствует в настройках.");
            var preferences = ReadPreferences();
            if (preferences.Get("Interface", "lang", "Ru") == selected.Code) return false;
            if (!IsLanguageAvailable(selected)) throw new InvalidOperationException("Не найден файл меню для выбранного языка.");
            preferences.Set("Interface", "lang", selected.Code); preferences.Set("Interface", "help", selected.HelpFile);
            SavePreferences(preferences); return true;
        }
        public void ApplyEngine(EngineOption engine)
        {
            if (engine == null || !GetEngines().Any(e => e.Code == engine.Code && e.Name == engine.Name)) throw new InvalidOperationException("Двигатель отсутствует в каталоге.");
            var preferences = ReadPreferences(); preferences.Set("Engine", "name", engine.Name); preferences.Set("Engine", "code", engine.Code.ToString(System.Globalization.CultureInfo.InvariantCulture));
            SavePreferences(preferences); settings.Set("Engine", "name", engine.Name); settings.Set("Engine", "code", engine.Code.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        private void SavePreferences(IniFile preferences)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
            string temporary = settingsPath + ".new-" + Guid.NewGuid().ToString("N");
            try { preferences.Save(temporary); if (File.Exists(settingsPath)) File.Replace(temporary, settingsPath, settingsPath + ".bak"); else File.Move(temporary, settingsPath); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        public ApplicationSettings(string dataDirectory, string preferencesPath)
        {
            DataDirectory = Path.GetFullPath(dataDirectory);
            settingsPath = Path.GetFullPath(preferencesPath);
            if (!File.Exists(Path.Combine(DataDirectory, "models.ini"))) throw new FileNotFoundException("В папке данных нет models.ini.");
            settings = IniFile.Load(File.Exists(settingsPath) ? settingsPath : Path.Combine(DataDirectory, "models.ini"));
            var list = new List<LanguageOption>();
            int count = settings.GetInt("lang", "count");
            if (count < 0 || count > 10) throw new InvalidDataException("Недопустимое число языков.");
            for (int i = 1; i <= count; i++)
            {
                string code = settings.Get("lang", "nn" + i);
                ValidateFolderName(code);
                list.Add(new LanguageOption(settings.Get("lang", "n" + i), code, settings.Get("lang", "help" + i)));
            }
            Languages = list.AsReadOnly();
            ValidateFolderName(Language);
        }
        public IReadOnlyList<EngineOption> GetEngines(string language = null)
        {
            string code = language ?? Language; ValidateFolderName(code);
            var jobs = IniFile.Load(Path.Combine(DataDirectory, code, "jobs.ini"));
            int count = jobs.GetInt("Engine", "count");
            if (count < 0 || count > 100) throw new InvalidDataException("Недопустимое число двигателей.");
            var list = new List<EngineOption>();
            for (int i = 1; i <= count; i++)
            {
                string name = jobs.Get("Engine", "n" + i); ValidateFolderName(name);
                int engineCode = jobs.GetInt("Engine", "nn" + i);
                if (engineCode < 1 || engineCode > 100 || list.Any(e => e.Code == engineCode)) throw new InvalidDataException("Недопустимый код двигателя.");
                list.Add(new EngineOption(name, jobs.Get("Engine", "ndescript" + i), engineCode));
            }
            return list.AsReadOnly();
        }
        public void Apply(LanguageOption language, EngineOption engine)
        {
            if (language == null || engine == null) throw new ArgumentNullException();
            if (!Languages.Any(l => l.Code == language.Code) || !GetEngines(language.Code).Any(e => e.Name == engine.Name && e.Code == engine.Code))
                throw new InvalidOperationException("Язык или двигатель отсутствует в каталоге.");
            settings.Set("Interface", "lang", language.Code); settings.Set("Interface", "help", language.HelpFile);
            settings.Set("Engine", "name", engine.Name); settings.Set("Engine", "code", engine.Code.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)); settings.Save(settingsPath);
        }
        private static void ValidateFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidDataException("Недопустимое имя каталога в настройках.");
        }
    }
}
