using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit15Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp = Path.Combine(Path.GetTempPath(), "Unit15Checks-" + Guid.NewGuid()); Directory.CreateDirectory(temp);
            try
            {
                var model = new InputFile41 { Comment = "Проверка" };
                for (int r = 0; r < 6; r++) for (int c = 0; c < 9; c++) model.Values[r,c] = (r * 9 + c - 20) / 100d;
                model.Parameters[0] = 51; model.Parameters[1] = 100; model.Parameters[8] = 67;
                string path = Path.Combine(temp, "41.dat"); model.Save(path, CultureInfo.InvariantCulture);
                var lines = File.ReadAllLines(path, LegacyCodec.TextEncoding);
                Check(lines.Length == 8 && lines.Skip(1).All(l => l.Length == 108) && lines[1].Substring(0,12) == "    -0.20000", "Unit15 six rows nine F5 fields width 12");
                Check(lines[7].Substring(0,24) == "           1          50", "Unit15 parameter offset minus 50");
                var copy = InputFile41.Load(path, CultureInfo.GetCultureInfo("ru-RU"));
                Check(copy.Values.Cast<double>().SequenceEqual(model.Values.Cast<double>()) && copy.Parameters.SequenceEqual(model.Parameters), "Unit15 dot values and parameter round trip");
                model.Save(path, CultureInfo.GetCultureInfo("ru-RU"));
                Check(InputFile41.Load(path, CultureInfo.GetCultureInfo("ru-RU")).Values.Cast<double>().SequenceEqual(model.Values.Cast<double>()) && File.Exists(path + ".bak"), "Unit15 comma values and backup");
                byte[] before = File.ReadAllBytes(path); model.Values[5,8] = 123456789;
                Reject(() => model.Save(path), "Unit15 overflow rejected"); Check(before.SequenceEqual(File.ReadAllBytes(path)), "Unit15 invalid save preserves original");
                model.Values[5,8] = 0; model.Parameters[0] = 50; Reject(() => model.Save(path), "Unit15 invalid parameter rejected");
                File.WriteAllText(path, "41 Short\r\n", LegacyCodec.TextEncoding);
                Check(InputFile41.Load(path).Values.Cast<double>().All(v => v == 0), "Unit15 missing rows default to zero");
                File.WriteAllText(path, "40 Wrong\r\n", LegacyCodec.TextEncoding); Reject(() => InputFile41.Load(path), "Unit15 wrong file type rejected");
                if (sourceRoot != null)
                {
                    string original = Path.Combine(sourceRoot,"sp-12","data","TEPL.DAT");
                    var real = InputFile41.Load(original, CultureInfo.InvariantCulture);
                    Check(real.Values[0,0] == 10.2 && real.Values[5,8] == 450 && real.Parameters[0] == 67 && real.Parameters[3] == 0, "Unit15 original TEPL values and IDs");
                    real.Save(path, CultureInfo.InvariantCulture);
                    Check(File.ReadAllBytes(path).SequenceEqual(File.ReadAllBytes(original)), "Unit15 original TEPL byte exact round trip");
                }
            }
            finally { Directory.Delete(temp,true); }
        }
        private static void Reject(Action action, string name) { try { action(); } catch (InvalidDataException) { Check(true,name); return; } throw new Exception(name); }
        private static void Check(bool value, string name) { if(!value)throw new Exception(name); Console.WriteLine("PASS " + name); }
    }
}
