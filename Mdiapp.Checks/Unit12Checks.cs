using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit12Checks
    {
        public static void Run(string sourceRoot)
        {
            string root = Path.Combine(Path.GetTempPath(), "Unit12Checks-" + Guid.NewGuid()); Directory.CreateDirectory(root);
            try
            {
                var model = new InputFile30 { Comment = "Проверка" };
                for (int row = 0; row < 15; row++) { model.Captions[row] = "Строка " + (row + 1); for (int column = 0; column < model.Lines[row].Length; column++) model.Lines[row][column] = (row + column + 1) / 100d; }
                int[] numbers = {186, 187, 191, 205, 209, 69}; Array.Copy(numbers, model.Parameters, 6);
                string dat = Path.Combine(root, "input30.dat"), ini = Path.Combine(root, "inputfiles.ini");
                File.WriteAllText(ini, ";comment\r\n[31_32]\r\ncount=27\r\ne1=1\r\nn7=538\r\n[Other]\r\nkeep=yes\r\n");
                model.Save(dat, ini, CultureInfo.InvariantCulture);
                var lines = File.ReadAllLines(dat, LegacyCodec.TextEncoding);
                Check(lines.Length == 16 && lines[1].StartsWith("    0.010    0.020    0.030"), "Unit12 15 rows and 9-wide F3 fields");
                Check(lines[4].StartsWith("  0.04000  0.05000  0.06000"), "Unit12 F5 precision on rows 4-9");
                Check(lines[3].StartsWith("      186      191      209") && lines[10].StartsWith("               187               205                69"), "Unit12 interleaved parameters and 18-wide secondary IDs");
                Check(lines[11].Length == 54 && !lines[11].Contains("!"), "Unit12 six F3 fields without trailing captions");
                var loaded = InputFile30.Load(dat, CultureInfo.InvariantCulture);
                Check(loaded.Parameters.SequenceEqual(numbers) && loaded.Lines[14].SequenceEqual(model.Lines[14]), "Unit12 numeric and parameter round trip");
                var definitions = IniFile.Load(ini);
                Check(Enumerable.Range(0, 6).All(i => definitions.GetInt("31_32", "n" + (i + 1)) == numbers[i]), "Unit12 updates six 31-32 parameter numbers");
                Check(definitions.GetInt("31_32", "count") == 27 && definitions.GetInt("31_32", "n7") == 538 && definitions.Get("Other", "keep") == "yes", "Unit12 retains other settings");
                var before = File.ReadAllBytes(dat); model.Lines[0][0] = 123456789;
                ExpectFailure(() => model.Save(dat, ini), "Unit12 field overflow rejected");
                Check(before.SequenceEqual(File.ReadAllBytes(dat)), "Unit12 overflow does not change DAT"); model.Lines[0][0] = 0.01;
                model.Comment = "Changed";
                using (var locked = new FileStream(ini, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) ExpectFailure(() => model.Save(dat, ini), "Unit12 locked settings prevent save");
                Check(before.SequenceEqual(File.ReadAllBytes(dat)), "Unit12 DAT remains unchanged on settings failure");
                model.Save(dat, ini, CultureInfo.GetCultureInfo("ru-RU"));
                Check(InputFile30.Load(dat, CultureInfo.GetCultureInfo("ru-RU")).Lines[0][0] == 0.01, "Unit12 decimal comma compatibility");
            }
            finally { Directory.Delete(root, true); }
        }
        private static void ExpectFailure(Action action, string label) { try { action(); } catch (InvalidDataException) { Console.WriteLine("PASS " + label); return; } catch (IOException) { Console.WriteLine("PASS " + label); return; } throw new Exception(label); }
        private static void Check(bool condition, string label) { if (!condition) throw new Exception(label); Console.WriteLine("PASS " + label); }
    }
}
