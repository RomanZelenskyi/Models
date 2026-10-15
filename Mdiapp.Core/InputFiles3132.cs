using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    public sealed class Input3132Row
    {
        public int Number { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
    }
    public sealed class PregValue
    {
        public int Code { get; set; }
        public int Parameter { get; set; }
        public double Value { get; set; }
    }
    public sealed class InputFiles3132
    {
        public string Comment31 { get; set; }
        public string Comment32 { get; set; }
        public string PregTitle { get; set; }
        public List<Input3132Row> Rows { get; } = new List<Input3132Row>();
        public List<PregValue> Preg { get; } = new List<PregValue>();
        public int Divider { get; private set; }
        public int Condition { get; set; }
        public double[] ConditionValues { get; } = new double[3];
        public static double Parse(string text, CultureInfo culture)
        {
            text = text.Trim();
            if (double.TryParse(text, NumberStyles.Float, culture, out double value)) return value;
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : 0;
        }
        private static string Field(string text, int start, int length) => start >= text.Length ? "" : text.Substring(start, Math.Min(length, text.Length - start));
        private static string Token(string text) => text.Split(new[] {' ', '\t', '!'}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        public static InputFiles3132 Load(string file31, string file32, IniFile definitions, IReadOnlyList<Unit3Parameter> parameters, string pregTitle, CultureInfo culture = null)
        {
            culture = culture ?? CultureInfo.CurrentCulture;
            var lines31 = File.ReadAllLines(file31, LegacyCodec.TextEncoding); var lines32 = File.ReadAllLines(file32, LegacyCodec.TextEncoding);
            var result = new InputFiles3132 { Comment31 = Header(lines31, "31"), Comment32 = Header(lines32, "32"), PregTitle = pregTitle };
            int count = definitions.GetInt("31_32", "count");
            if (count < 0 || count > 100) throw new InvalidDataException("Недопустимое количество параметров 31–32.");
            for (int i = 1; i <= count; i++)
            {
                int number = definitions.GetInt("31_32", "n" + i); bool enabled = definitions.GetInt("31_32", "e" + i) == 1;
                if (number < 0 || number > 2000) throw new InvalidDataException("Неверный номер параметра 31–32.");
                if (i <= 6 && enabled) result.Divider++;
                if (number == 0 && !enabled) continue;
                var parameter = parameters.FirstOrDefault(p => p.Number == number);
                if (number != 0 && parameter == null) throw new InvalidDataException("Нет параметра " + number + " в справочнике.");
                result.Rows.Add(new Input3132Row { Number = number, Symbol = parameter?.Symbol ?? "rezerv", Name = parameter?.Name ?? "Резерв" });
            }
            if (lines31.Length < result.Rows.Count + 1 || lines32.Length < result.Rows.Count + 1) throw new InvalidDataException("Недостаточно строк данных в паре 31–32.");
            for (int i = 0; i < result.Rows.Count; i++)
            {
                var row = result.Rows[i];
                row.Value = row.Number == 0 ? Parse(definitions.Get("31_32", "r", "0.5"), culture) : Parse(Field(lines31[i + 1], 0, 14), culture);
                row.Minimum = row.Number == 0 ? Parse(definitions.Get("31_32", "r1", "0"), culture) : Parse(Field(lines32[i + 1], 0, 14), culture);
                row.Maximum = row.Number == 0 ? Parse(definitions.Get("31_32", "r2", "1"), culture) : Parse(Field(lines32[i + 1], 14, 15), culture);
            }
            int cursor = result.Rows.Count + 2; // Skip the PREG caption after numeric rows.
            Func<string> next = () => cursor < lines31.Length ? lines31[cursor++] : "";
            for (int i = 0; i <= 3; i++)
            {
                if (definitions.GetInt("31_32", "preg" + i) != 1) continue;
                int.TryParse(Token(next()), out int parameter);
                result.Preg.Add(new PregValue { Code = i, Parameter = parameter, Value = Parse(Token(next()), culture) });
            }
            int.TryParse(Token(next()), out int condition); result.Condition = condition;
            string[] values = next().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < 3; i++) result.ConditionValues[i] = i < values.Length ? Parse(values[i], culture) : 0;
            return result;
        }
        private static string Header(string[] lines, string type)
        {
            if (lines.Length == 0) throw new InvalidDataException("Файл пуст.");
            string header = lines[0].TrimStart(); int space = header.IndexOf(' ');
            if ((space < 0 ? header : header.Substring(0, space)) != type) throw new InvalidDataException("Ожидается файл типа " + type);
            return space < 0 ? "" : header.Substring(space + 1);
        }
        private static string Fixed(double value, CultureInfo culture)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidDataException("Значение должно быть конечным числом.");
            string result = value.ToString("F6", culture).PadLeft(14);
            if (result.Length > 14) throw new InvalidDataException("Число не помещается в поле из 14 символов.");
            return result;
        }
        public void Validate()
        {
            foreach (var row in Rows)
            {
                if (row.Value < row.Minimum) throw new InvalidDataException("Параметр " + row.Number + ": начальное значение меньше нижнего предела.");
                if (row.Value > row.Maximum) throw new InvalidDataException("Параметр " + row.Number + ": начальное значение больше верхнего предела.");
            }
            foreach (string text in new[] {Comment31, Comment32, PregTitle}.Concat(Rows.Select(r => r.Symbol + " " + r.Name)))
                if (text != null && text.IndexOfAny(new[] {'\r', '\n'}) >= 0) throw new InvalidDataException("Подпись должна занимать одну строку.");
            if (Condition < 0 || Condition > 30 || Preg.Any(p => p.Parameter < 0 || p.Parameter > 2000)) throw new InvalidDataException("Недопустимый номер условия или параметра PREG.");
            if (Preg.Any(p => double.IsNaN(p.Value) || double.IsInfinity(p.Value))) throw new InvalidDataException("Значение PREG должно быть конечным числом.");
        }
        public void Save(string file31, string file32, CultureInfo culture = null)
        {
            culture = culture ?? CultureInfo.CurrentCulture; Validate();
            file31 = Path.GetFullPath(file31); file32 = Path.GetFullPath(file32);
            if (string.Equals(file31, file32, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Файлы 31 и 32 должны иметь разные имена.");
            var a = new List<string> {"31 " + Comment31}; var b = new List<string> {"32 " + Comment32};
            foreach (var row in Rows)
            {
                a.Add(Fixed(row.Value, culture) + "      !" + row.Symbol + " " + row.Name);
                b.Add(Fixed(row.Minimum, culture) + " " + Fixed(row.Maximum, culture) + "     !" + row.Symbol + " " + row.Name);
            }
            a.Add(PregTitle ?? "");
            foreach (var preg in Preg) { a.Add(preg.Parameter + "          !nreg"); a.Add(preg.Value.ToString("G", culture) + "          !PREG " + preg.Code); }
            a.Add(Condition.ToString()); a.Add(string.Join(" ", ConditionValues.Select(value => Fixed(value, culture))));
            string first = file31 + ".new-" + Guid.NewGuid().ToString("N"), second = file32 + ".new-" + Guid.NewGuid().ToString("N");
            byte[] previous = File.Exists(file31) ? File.ReadAllBytes(file31) : null; bool committed = false;
            try
            {
                File.WriteAllText(first, string.Join("\r\n", a) + "\r\n", LegacyCodec.TextEncoding);
                File.WriteAllText(second, string.Join("\r\n", b) + "\r\n", LegacyCodec.TextEncoding);
                Commit(first, file31); committed = true; Commit(second, file32);
            }
            catch
            {
                if (committed) { if (previous != null) File.WriteAllBytes(file31, previous); else File.Delete(file31); }
                throw;
            }
            finally { if (File.Exists(first)) File.Delete(first); if (File.Exists(second)) File.Delete(second); }
        }
        private static void Commit(string temporary, string target) { if (File.Exists(target)) File.Replace(temporary, target, target + ".bak"); else File.Move(temporary, target); }
    }
}
