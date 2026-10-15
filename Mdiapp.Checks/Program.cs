using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
internal static class Program
{
static void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS " + name);
}
[STAThread]
static void Main(string[] args)
{
if(args.Length>0&&args[0]=="--solver-fixture"){File.WriteAllText("param.dat","0 10\r\n1 20\r\n");File.WriteAllText("rez.dat","1");return;}
if(args.Length>0&&args[0]=="--solver-wait"){System.Threading.Thread.Sleep(30000);return;}
Unit4Checks.Run();
Unit5Checks.Run();
Unit6Checks.Run();
Unit7Checks.Run();
Unit8Checks.Run();
Unit9Checks.Run();
Unit10Checks.Run();
Unit11Checks.Run(args.Length > 0 ? Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName : null);
Unit12Checks.Run(args.Length > 0 ? Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName : null);
Unit13Checks.Run(args.Length > 0 ? Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName : null);
Check(ParameterSeries.Calculate(new double[] {0, 1, 2}, SeriesMode.Constant, 3).SequenceEqual(new double[] {3d, 3, 3}), "Unit2 constant");
Check(ParameterSeries.Calculate(new double[] {0, 100, 200}, SeriesMode.Increment, 2, 0.5).SequenceEqual(new double[] {2d, 2.5, 3}), "Unit2 increment is per point");
Check(ParameterSeries.Calculate(new double[] {0, 1, 2}, SeriesMode.Power, 10, linear: 2, coefficient: 3, exponent: 2).SequenceEqual(new double[] {13d, 15, 26}), "Unit2 power including t=0");
using var source = new MemoryStream(LegacyCodec.TextEncoding.GetBytes("AБ\r\n\r\nZ\r\n"));
using var coded = new MemoryStream();
LegacyCodec.Encode(source, coded);
byte[] fixture = new byte[] {66,0,0,0,198,0,0,0,247,252,255,255,247,252,255,255,91,0,0,0,247,252,255,255};
Check(coded.ToArray().SequenceEqual(fixture), "Unit41 matches hand-calculated Delphi Int32 fixture");
coded.Position = 0;
using var decoded = new MemoryStream(); LegacyCodec.Decode(coded, decoded);
Check(decoded.ToArray().SequenceEqual(source.ToArray()), "Unit41 Cyrillic and empty line round trip");
try { using var bad = new MemoryStream(new byte[] {1, 2}); LegacyCodec.Decode(bad, new MemoryStream()); throw new Exception("Truncation accepted"); }
catch (InvalidDataException) { Console.WriteLine("PASS truncated COD rejected"); }
var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ini");
try
{
    var ini = new IniFile(); ini.Set("Interface", "lang", "Ru"); ini.Set("forms", "n2", "Модель"); ini.Save(path);
    var loaded = IniFile.Load(path);
    Check(loaded.Get("INTERFACE", "LANG") == "Ru" && loaded.Get("forms", "n2") == "Модель", "INI case and Windows-1251");
}
finally { File.Delete(path); }
var projectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".inm");
try
{
    var fixtureProject = LegacyCodec.TextEncoding.GetBytes("Пример\r\nCODE\r\n   3\r\n   1\r\n..\\data\\a.dat\r\n   1   2   3\r\nНеизвестные секции\r\n");
    File.WriteAllBytes(projectPath, fixtureProject);
    var project = LegacyProject.Load(projectPath);
    Check(project.ProjectKey == 3 && project.IncludedFiles.Single() == "..\\data\\a.dat", "INM header");
    project.Save(projectPath);
    Check(File.ReadAllBytes(projectPath).SequenceEqual(fixtureProject), "INM unchanged byte-for-byte");
    project.Remark = "Изменено"; project.Save(projectPath);
    var changed = File.ReadAllBytes(projectPath);
    Check(changed.Skip(Array.IndexOf(changed, (byte)10)).SequenceEqual(fixtureProject.Skip(Array.IndexOf(fixtureProject, (byte)10))), "INM unknown sections retained");
}
finally { File.Delete(projectPath); }
var settingsRoot = Path.Combine(Path.GetTempPath(), "MdiappChecks-" + Guid.NewGuid());
Directory.CreateDirectory(Path.Combine(settingsRoot, "Ru"));
Directory.CreateDirectory(Path.Combine(settingsRoot, "En"));
try
{
    string configPath = Path.Combine(settingsRoot, "models.ini");
    File.WriteAllText(configPath, "[Interface]\r\nlang=Ru\r\n[lang]\r\ncount=2\r\nn1=Русский\r\nnn1=Ru\r\nhelp1=manual.pdf\r\nn2=English\r\nnn2=En\r\nhelp2=guide.pdf\r\n[Engine]\r\nname=sp-12\r\ncode=3\r\n", LegacyCodec.TextEncoding);
    string engineFixture = "[Engine]\r\ncount=2\r\nn1=sp-12\r\nnn1=3\r\nndescript1=Первый\r\nn2=other\r\nnn2=17\r\nndescript2=Второй\r\n";
    foreach (string code in new[] {"Ru", "En"}) File.WriteAllText(Path.Combine(settingsRoot, code, "jobs.ini"), engineFixture, LegacyCodec.TextEncoding);
    var before = File.ReadAllBytes(configPath);
    string preferences = Path.Combine(settingsRoot, "Preferences", "models.ini");
    var settingsModel = new ApplicationSettings(settingsRoot, preferences);
    Check(settingsModel.GetEngines()[1].Code == 17, "Unit10 sparse engine codes");
    settingsModel.Apply(settingsModel.Languages[1], settingsModel.GetEngines("En")[1]);
    var reloadedSettings = new ApplicationSettings(settingsRoot, preferences);
    Check(reloadedSettings.Language == "En" && reloadedSettings.Engine == "other" && reloadedSettings.EngineCode == 17, "Unit7/10 settings persist");
    Check(IniFile.Load(preferences).Get("Interface", "help") == "guide.pdf", "Unit7 help follows language");
    Check(File.ReadAllBytes(configPath).SequenceEqual(before), "Original settings untouched");
    var numericCulture = System.Globalization.CultureInfo.InvariantCulture;
    var input51 = new InputFile51 { Comment = "Пример" };
    input51.Values[0] = 1.25; input51.Values[1] = -2; input51.Values[2] = 0; input51.Values[3] = 123;
    string inputPath = Path.Combine(settingsRoot, "51.dat"); input51.Save(inputPath, numericCulture);
    Check(File.ReadAllText(inputPath, LegacyCodec.TextEncoding) == "51 Пример\r\n   1.250  -2.000   0.000 123.000\r\n", "Unit21 fixed-width output");
    Check(InputFile51.Load(inputPath, numericCulture).Values.SequenceEqual(input51.Values), "Unit21 round trip");
    input51.Values[0] = 123456;
    try { input51.Save(inputPath, numericCulture); throw new Exception("Overflow accepted"); }
    catch (InvalidDataException) { Console.WriteLine("PASS Unit21 overflow rejected"); }
    var commaCulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
    input51.Values[0] = 1.25; input51.Save(inputPath, commaCulture);
    Check(InputFile51.Load(inputPath, commaCulture).Values.SequenceEqual(input51.Values), "Unit21 decimal comma");
}
finally { Directory.Delete(settingsRoot, true); }
if (args.Length > 0)
{
    Unit25Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit27Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit28Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit29Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit30Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit31Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit32Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    ProjectChecks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit42Checks.Run();
    Unit40Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit39Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit38Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit37Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit36Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit35Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit34Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit33Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit26Checks.Run();
    Unit24Checks.Run();
    Unit23Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit22Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit20Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit19Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit18Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit17Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit16Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit15Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit14Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    Unit3Checks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
    string volume = Path.Combine(Directory.GetParent(args[0]).FullName, "data", "volume.dat");
    if (File.Exists(volume))
    {
        var model = InputFile51.Load(volume, System.Globalization.CultureInfo.InvariantCulture);
        Check(model.Values.SequenceEqual(new double[] {0.01, 0, 0.1, 0.1}), "Real Unit21 volume.dat");
    }
    foreach (var input in Directory.EnumerateFiles(args[0], "*.inm"))
    {
        var project = LegacyProject.Load(input);
        var temporary = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".inm");
        try { project.Save(temporary); Check(File.ReadAllBytes(input).SequenceEqual(File.ReadAllBytes(temporary)), "Real INM " + Path.GetFileName(input)); }
        finally { File.Delete(temporary); }
    }
    WpfChecks.Run(Directory.GetParent(Directory.GetParent(args[0]).FullName).FullName);
}

}
}
}
