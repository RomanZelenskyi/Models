using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Mdiapp.Core
{
    public sealed class InputFile30
    {
        public string Comment { get; set; }
        public double[][] Lines { get; } = Enumerable.Range(0, 15).Select(row => new double[row >= 10 ? 6 : 3]).ToArray();
        public int[] Parameters { get; } = new int[6];
        public string[] Captions { get; } = new string[15];
        public static InputFile30 Load(string path, CultureInfo culture = null)
        {
            culture = culture ?? CultureInfo.CurrentCulture;
            var lines = File.ReadAllLines(path, LegacyCodec.TextEncoding);
            if (lines.Length < 16) throw new InvalidDataException("Файл типа 30 должен содержать заголовок и 15 строк данных.");
            string header = lines[0].TrimStart(); int space = header.IndexOf(' ');
            if ((space < 0 ? header : header.Substring(0, space)) != "30") throw new InvalidDataException("Ожидается файл типа 30.");
            var model = new InputFile30 { Comment = space < 0 ? "" : header.Substring(space + 1) };
            for (int row = 0; row < 15; row++)
            {
                int width = row == 9 ? 18 : 9;
                for (int column = 0; column < model.Lines[row].Length; column++)
                {
                    int start = column * width; string text = start < lines[row + 1].Length ? lines[row + 1].Substring(start, Math.Min(width, lines[row + 1].Length - start)).Trim() : "";
                    if (row == 2 || row == 9) { int.TryParse(text, out int parameter); model.Parameters[column * 2 + (row == 9 ? 1 : 0)] = parameter; }
                    else model.Lines[row][column] = InputFiles3132.Parse(text, culture);
                }
                int marker = lines[row + 1].IndexOf('!'); model.Captions[row] = marker >= 0 ? lines[row + 1].Substring(marker + 1) : "";
            }
            return model;
        }
        public string Serialize(CultureInfo culture = null)
        {
            culture = culture ?? CultureInfo.CurrentCulture;
            if (Parameters.Any(p => p < 0 || p > 2000)) throw new InvalidDataException("Номер параметра должен быть от 0 до 2000.");
            if (new[] {Comment}.Concat(Captions).Any(text => text != null && text.IndexOfAny(new[] {'\r', '\n'}) >= 0)) throw new InvalidDataException("Комментарий должен занимать одну строку.");
            var output = new List<string> {"30 " + Comment};
            for (int row = 0; row < 15; row++)
            {
                string line = "";
                for (int column = 0; column < Lines[row].Length; column++)
                {
                    int width = row == 9 ? 18 : 9; string field;
                    if (row == 2 || row == 9) field = Parameters[column * 2 + (row == 9 ? 1 : 0)].ToString(CultureInfo.InvariantCulture);
                    else
                    {
                        double value = Lines[row][column]; if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidDataException("Значение должно быть конечным числом.");
                        field = value.ToString(row >= 3 && row <= 8 ? "F5" : "F3", culture);
                    }
                    if (field.Length > width) throw new InvalidDataException("Строка " + (row + 1) + ", поле " + (column + 1) + ": число не помещается в поле.");
                    line += field.PadLeft(width);
                }
                if (row < 10) line += "     !" + Captions[row]; output.Add(line);
            }
            return string.Join("\r\n", output) + "\r\n";
        }
        public void Save(string dataPath, string definitionsPath, CultureInfo culture = null)
        {
            dataPath = Path.GetFullPath(dataPath); definitionsPath = Path.GetFullPath(definitionsPath);
            if (string.Equals(dataPath, definitionsPath, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("DAT и INI должны быть разными файлами.");
            string text = Serialize(culture); var ini = IniFile.Load(definitionsPath);
            for (int i = 0; i < 6; i++) ini.Set("31_32", "n" + (i + 1), Parameters[i].ToString(CultureInfo.InvariantCulture));
            string tempData = dataPath + ".new-" + Guid.NewGuid().ToString("N"), tempIni = definitionsPath + ".new-" + Guid.NewGuid().ToString("N");
            byte[] before = File.Exists(dataPath) ? File.ReadAllBytes(dataPath) : null; bool changed = false;
            try
            {
                File.WriteAllText(tempData, text, LegacyCodec.TextEncoding); ini.Save(tempIni);
                Commit(tempData, dataPath); changed = true; Commit(tempIni, definitionsPath);
            }
            catch { if (changed) { if (before != null) File.WriteAllBytes(dataPath, before); else File.Delete(dataPath); } throw; }
            finally { if (File.Exists(tempData)) File.Delete(tempData); if (File.Exists(tempIni)) File.Delete(tempIni); }
        }
        private static void Commit(string temp, string target) { if (File.Exists(target)) File.Replace(temp, target, target + ".bak"); else File.Move(temp, target); }
    }
}
