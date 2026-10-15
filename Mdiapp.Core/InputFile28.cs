using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    public sealed class Input28Column
    {
        public int PhysicalIndex { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
    }
    public sealed class Input28Row
    {
        public int PhysicalIndex { get; set; }
        public string Name { get; set; }
    }
    public sealed class InputFile28
    {
        public int Type { get; private set; }
        public string Comment { get; set; }
        public int PhysicalRowCount { get; private set; }
        public List<Input28Column> Columns { get; } = new List<Input28Column>();
        public List<Input28Row> Rows { get; } = new List<Input28Row>();
        public double[,] Values { get; } = new double[20, 15];
        public double[] Power { get; } = new double[3];
        public bool[] PowerEnabled { get; } = new bool[3];
        public string[] PowerNames { get; } = new string[3];
        public IReadOnlyList<string> TrailingLines { get; private set; } = Array.Empty<string>();
        public static InputFile28 Load(string path, IniFile definitions, CultureInfo culture = null)
        {
            culture = culture ?? CultureInfo.CurrentCulture; var text = File.ReadAllLines(path, LegacyCodec.TextEncoding);
            if (text.Length == 0) throw new InvalidDataException("Файл пуст.");
            string header = text[0].TrimStart(); int separator = header.IndexOf(' ');
            if (!int.TryParse(separator < 0 ? header : header.Substring(0, separator), out int type) || !new[] {28, 281, 282}.Contains(type)) throw new InvalidDataException("Ожидается тип 28, 281 или 282.");
            var model = new InputFile28 { Type = type, Comment = separator < 0 ? "" : header.Substring(separator + 1), PhysicalRowCount = definitions.GetInt("28", "n28count", 10) };
            if (model.PhysicalRowCount < 1 || model.PhysicalRowCount > 20) throw new InvalidDataException("Количество строк типа 28 должно быть от 1 до 20.");
            if (text.Length < model.PhysicalRowCount + 3) throw new InvalidDataException("Файл не содержит всех строк таблицы и отборов мощности.");
            for (int i = 0; i < 20; i++)
            {
                if (definitions.GetInt("28", "ce" + (i + 1)) == 1)
                {
                    if (i >= 15) throw new InvalidDataException("Формат содержит только 15 физических столбцов.");
                    model.Columns.Add(new Input28Column { PhysicalIndex = i, Position = definitions.GetInt("28", "nrow" + (i + 1)), Name = definitions.Get("28", "col" + (i + 1)) });
                }
                if (definitions.GetInt("28", "re" + (i + 1)) == 1)
                {
                    if (i >= model.PhysicalRowCount) throw new InvalidDataException("Включённая строка выходит за n28count.");
                    model.Rows.Add(new Input28Row { PhysicalIndex = i, Name = definitions.Get("28", "row" + (i + 1)) });
                }
            }
            model.Columns.Sort((a, b) => a.Position.CompareTo(b.Position));
            if (model.Columns.Where((column, i) => column.Position != i + 1).Any()) throw new InvalidDataException("Позиции включённых столбцов nrowN должны быть уникальными и последовательными от 1.");
            for (int row = 0; row < model.PhysicalRowCount; row++) for (int column = 0; column < 15; column++) model.Values[row, column] = InputFiles3132.Parse(Field(text[row + 2], column * 9, 9), culture);
            for (int i = 0; i < 3; i++) { model.Power[i] = InputFiles3132.Parse(Field(text[model.PhysicalRowCount + 2], i * 15, 15), culture); model.PowerEnabled[i] = definitions.GetInt("28", "ne" + (i + 1)) == 1; model.PowerNames[i] = definitions.Get("28", "n" + (i + 1)); }
            model.TrailingLines = text.Skip(model.PhysicalRowCount + 3).ToArray();
            return model;
        }
        private static string Field(string text, int start, int width) => start >= text.Length ? "" : text.Substring(start, Math.Min(width, text.Length - start));
        private static string Fixed(double value, int width, CultureInfo culture)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidDataException("Значение должно быть конечным числом.");
            string text = value.ToString("F5", culture); if (text.Length > width) throw new InvalidDataException("Число не помещается в поле шириной " + width + ".");
            return text.PadLeft(width);
        }
        public string Serialize(CultureInfo culture = null, IReadOnlyList<string> trailingLines = null)
        {
            culture = culture ?? CultureInfo.CurrentCulture;
            if ((Comment ?? "").IndexOfAny(new[] {'\r', '\n'}) >= 0) throw new InvalidDataException("Комментарий должен занимать одну строку.");
            var lines = new List<string> {Type + " " + Comment};
            string captions = "";
            for (int i = 0; i < 15; i++) { string name = Columns.FirstOrDefault(c => c.PhysicalIndex == i)?.Name ?? ""; if (name.IndexOfAny(new[] {'\r', '\n'}) >= 0) throw new InvalidDataException("Название столбца должно занимать одну строку."); captions += (name.Length > 9 ? name.Substring(0, 9) : name).PadLeft(9); }
            lines.Add(captions);
            for (int row = 0; row < PhysicalRowCount; row++)
            {
                string line = ""; for (int column = 0; column < 15; column++)
                {
                    // Unit13 clears disabled rows and columns on saving.
                    double value = Rows.Any(r => r.PhysicalIndex == row) && Columns.Any(c => c.PhysicalIndex == column) ? Values[row, column] : 0;
                    line += Fixed(value, 9, culture);
                }
                lines.Add(line);
            }
            lines.Add(string.Concat(Power.Select(value => Fixed(value, 15, culture))));
            lines.AddRange(trailingLines ?? TrailingLines);
            return string.Join("\r\n", lines) + "\r\n";
        }
        public void Save(string path, CultureInfo culture = null, IReadOnlyList<string> trailingLines = null)
        {
            string text = Serialize(culture, trailingLines); string temporary = path + ".new-" + Guid.NewGuid().ToString("N");
            try { File.WriteAllText(temporary, text, LegacyCodec.TextEncoding); if (File.Exists(path)) File.Replace(temporary, path, path + ".bak"); else File.Move(temporary, path); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
