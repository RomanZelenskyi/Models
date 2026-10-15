using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    // Unit15 input data; unrelated to the Unit41 COD codec.
    public sealed class InputFile41
    {
        public string Comment { get; set; } = "";
        public double[,] Values { get; } = new double[6, 9];
        public int[] Parameters { get; } = new int[9];
        private static string Field(string line, int column)
        {
            int offset = column * 12;
            return offset >= line.Length ? "" : line.Substring(offset, Math.Min(12, line.Length - offset)).Replace(" ", "");
        }
        public static InputFile41 Load(string path, CultureInfo culture = null)
        {
            culture = culture ?? CultureInfo.CurrentCulture;
            var lines = File.ReadAllLines(path, LegacyCodec.TextEncoding);
            if (lines.Length == 0) throw new InvalidDataException("Пустой файл типа 41.");
            string header = lines[0].TrimStart(); int space = header.IndexOf(' ');
            if ((space < 0 ? header : header.Substring(0, space)) != "41") throw new InvalidDataException("Это не файл типа 41.");
            var model = new InputFile41 { Comment = space < 0 ? "" : header.Substring(space + 1) };
            for (int row = 0; row < 6; row++) for (int col = 0; col < 9; col++)
                model.Values[row, col] = InputFiles3132.Parse(row + 1 < lines.Length ? Field(lines[row + 1], col) : "", culture);
            for (int col = 0; col < 9; col++)
            {
                int.TryParse(lines.Length > 7 ? Field(lines[7], col) : "", NumberStyles.Integer, CultureInfo.InvariantCulture, out int stored);
                if (stored < 0 || stored > 50) throw new InvalidDataException("Номер параметра файла 41 должен быть от 0 до 50.");
                model.Parameters[col] = stored == 0 ? 0 : stored + 50;
            }
            return model;
        }
        public void Save(string path, CultureInfo culture = null)
        {
            culture = culture ?? CultureInfo.CurrentCulture;
            if (Comment.IndexOfAny(new[] { '\r', '\n' }) >= 0) throw new InvalidDataException("Комментарий должен занимать одну строку.");
            var lines = new string[8]; lines[0] = "41 " + Comment;
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    double value = Values[row, col];
                    if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidDataException("Значение должно быть конечным числом.");
                    string field = value.ToString("F5", culture);
                    if (field.Length > 12) throw new InvalidDataException("Значение не помещается в поле из 12 символов.");
                    lines[row + 1] += field.PadLeft(12);
                }
            }
            if (Parameters.Any(p => p != 0 && (p < 51 || p > 100))) throw new InvalidDataException("Выберите параметр 51–100 или 0.");
            lines[7] = string.Concat(Parameters.Select(p => (p == 0 ? 0 : p - 50).ToString(CultureInfo.InvariantCulture).PadLeft(12)));
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
