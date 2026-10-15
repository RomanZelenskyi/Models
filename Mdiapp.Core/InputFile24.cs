using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    // Unit14: three F5 fields, one F5 field, then enabled F8 fields.
    public sealed class InputFile24
    {
        public string Comment { get; set; } = "";
        public double[] Values { get; } = new double[4];
        public string[] Names { get; } = new string[4];
        public bool[] Enabled { get; } = new bool[4];
        public double[] AdditionalValues { get; } = new double[15];
        public string[] AdditionalNames { get; } = new string[15];
        public bool[] AdditionalEnabled { get; } = new bool[15];
        public string GroupCaption { get; set; } = "";
        public string FourthCaption { get; set; } = "";

        public static InputFile24 Load(string path, IniFile definitions, IniFile menu, CultureInfo culture = null)
        {
            culture = culture ?? CultureInfo.CurrentCulture;
            var lines = File.ReadAllLines(path, LegacyCodec.TextEncoding);
            if (lines.Length < 2) throw new InvalidDataException("Файл типа 24 должен содержать заголовок и строку значений.");
            string header = lines[0].TrimStart();
            int space = header.IndexOf(' ');
            if ((space < 0 ? header : header.Substring(0, space)) != "24") throw new InvalidDataException("Это не файл типа 24.");
            var model = new InputFile24 { Comment = space < 0 ? "" : header.Substring(space + 1), GroupCaption = menu.Get("files", "n16"), FourthCaption = menu.Get("files", "n17") };
            for (int i = 0; i < 4; i++)
            {
                model.Names[i] = i == 3 ? model.FourthCaption : definitions.Get("24", "n" + (i + 1));
                model.Enabled[i] = definitions.GetInt("24", "ne" + (i + 1)) == 1;
                string line = i == 3 ? (lines.Length > 2 ? lines[2] : "") : lines[1];
                int offset = i == 3 ? 0 : i * 8;
                model.Values[i] = InputFiles3132.Parse(Field(line, offset, 8), culture);
            }
            int row = 3;
            for (int i = 0; i < 15; i++)
            {
                string key = (i + 11).ToString(CultureInfo.InvariantCulture);
                model.AdditionalNames[i] = definitions.Get("24", "n" + key);
                string flag = definitions.Get("24", "ne" + key);
                model.AdditionalEnabled[i] = flag == "1" || bool.TryParse(flag, out bool value) && value;
                if (model.AdditionalEnabled[i])
                {
                    model.AdditionalValues[i] = row < lines.Length ? InputFiles3132.Parse(Field(lines[row], 0, 12), culture) : 0;
                    row++;
                }
            }
            return model;
        }
        private static string Field(string line, int offset, int width) => offset >= line.Length ? "" : line.Substring(offset, Math.Min(width, line.Length - offset)).Replace(" ", "");
        private static string Format(double value, int width, int digits, CultureInfo culture)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidDataException("Значение должно быть конечным числом.");
            string text = value.ToString("F" + digits, culture);
            if (text.Length > width) throw new InvalidDataException("Значение не помещается в поле из " + width + " символов.");
            return text.PadLeft(width);
        }
        public void Save(string path, CultureInfo culture = null)
        {
            culture = culture ?? CultureInfo.CurrentCulture;
            if (new[] { Comment, GroupCaption, FourthCaption }.Concat(AdditionalNames).Any(s => s != null && s.IndexOfAny(new[] { '\r', '\n' }) >= 0)) throw new InvalidDataException("Комментарии должны занимать одну строку.");
            var lines = new List<string> { "24 " + Comment,
                string.Concat(Values.Take(3).Select(v => Format(v, 8, 5, culture))) + "        !" + GroupCaption,
                Format(Values[3], 8, 5, culture) + "                        !" + FourthCaption };
            for (int i = 0; i < 15; i++) if (AdditionalEnabled[i]) lines.Add(Format(AdditionalValues[i], 12, 8, culture) + "                        !" + AdditionalNames[i]);
            string temporary = path + ".new-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllText(temporary, string.Join("\r\n", lines) + "\r\n", LegacyCodec.TextEncoding);
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak"); else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
