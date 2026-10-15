using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    // Unit21: four fields of eight characters, three decimal places.
    public sealed class InputFile51
    {
        public string Comment { get; set; } = "";
        public double[] Values { get; } = new double[4];
        public static InputFile51 Load(string path, CultureInfo culture)
        {
            var lines = File.ReadAllLines(path, LegacyCodec.TextEncoding);
            if (lines.Length < 2) throw new InvalidDataException("Файл типа 51 должен содержать две строки.");
            string header = lines[0].TrimStart();
            int space = header.IndexOf(' ');
            if ((space < 0 ? header : header.Substring(0, space)) != "51") throw new InvalidDataException("Это не файл типа 51.");
            var result = new InputFile51 { Comment = space < 0 ? "" : header.Substring(space + 1) };
            var fields = lines[1].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            // Delphi accepts invalid numbers as zero.
            for (int i = 0; i < 4; i++) result.Values[i] = i < fields.Length ? InputFiles3132.Parse(fields[i], culture) : 0;
            return result;
        }
        public void Save(string path, CultureInfo culture)
        {
            if (Comment.IndexOfAny(new[] { '\r', '\n' }) >= 0) throw new InvalidDataException("Комментарий должен занимать одну строку.");
            string fields = string.Concat(Values.Select(value =>
            {
                if (double.IsInfinity(value) || double.IsNaN(value)) throw new InvalidDataException("Значение должно быть конечным числом.");
                string field = value.ToString("F3", culture).PadLeft(8);
                // Delphi keeps the last eight characters even on overflow; reject to avoid silently corrupting input.
                if (field.Length > 8) throw new InvalidDataException("Значение не помещается в поле из 8 символов.");
                return field;
            }));
            File.WriteAllText(path, "51 " + Comment + "\r\n" + fields + "\r\n", LegacyCodec.TextEncoding);
        }
    }
}
