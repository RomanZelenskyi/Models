using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace Mdiapp.Core
{

public sealed class IniFile
{
    private readonly Dictionary<string, Dictionary<string,string>> sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
    private string[] originalLines;
    public static IniFile Load(string path)
    {
        var result = new IniFile();
        if (!File.Exists(path)) return result;
        string section = "";
        result.originalLines = File.ReadAllLines(path, LegacyCodec.TextEncoding);
        foreach (var raw in result.originalLines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(";", StringComparison.Ordinal)) continue;
            if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal)) { section = line.Substring(1, line.Length - 2); continue; }
            int separator = line.IndexOf('=');
            if (separator >= 0) result.Set(section, line.Substring(0, separator).Trim(), line.Substring(separator + 1).Trim());
        }
        return result;
    }
    public string Get(string section, string key, string fallback = "") =>
        sections.TryGetValue(section, out var values) && values.TryGetValue(key, out var value) ? value : fallback;
    public int GetInt(string section, string key, int fallback = 0) => int.TryParse(Get(section,key), out int value) ? value : fallback;
    public void Set(string section, string key, string value)
    {
        if (!sections.TryGetValue(section, out var values)) sections[section] = values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        values[key] = value;
    }
    public IEnumerable<string> SectionNames => sections.Keys.ToArray();
    public IEnumerable<KeyValuePair<string, string>> Entries(string section) =>
        sections.TryGetValue(section, out var values) ? values.ToArray() : Array.Empty<KeyValuePair<string, string>>();
    public void RemoveSection(string section) => sections.Remove(section);
    public void Save(string path)
    {
        using var writer = new StreamWriter(path, false, LegacyCodec.TextEncoding);
        writer.NewLine = "\r\n";
        if (originalLines != null)
        {
            var written = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            string active = "";
            Action flush = () =>
            {
                if (!sections.TryGetValue(active, out var values)) return;
                if (!written.TryGetValue(active, out var keys)) written[active] = keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in values) if (keys.Add(item.Key)) writer.WriteLine(item.Key + "=" + item.Value);
            };
            foreach (string raw in originalLines)
            {
                string line = raw.Trim();
                if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
                { flush(); active = line.Substring(1, line.Length - 2); if (sections.ContainsKey(active)) writer.WriteLine(raw); continue; }
                int separator = line.IndexOf('=');
                if (separator >= 0 && !line.StartsWith(";", StringComparison.Ordinal))
                {
                    string key = line.Substring(0, separator).Trim();
                    if (!sections.TryGetValue(active, out var values) || !values.TryGetValue(key, out string value)) continue;
                    if (!written.TryGetValue(active, out var keys)) written[active] = keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    keys.Add(key);
                    // Preserve unchanged lines, spacing, and comments.
                    writer.WriteLine(line.Substring(separator + 1).Trim() == value ? raw : key + "=" + value);
                }
                else writer.WriteLine(raw);
            }
            flush();
            foreach (var section in sections.Where(s => !written.ContainsKey(s.Key)))
            {
                writer.WriteLine("[" + section.Key + "]");
                foreach (var item in section.Value) writer.WriteLine(item.Key + "=" + item.Value);
            }
            return;
        }
        foreach (var section in sections)
        {
            writer.WriteLine($"[{section.Key}]");
            foreach (var item in section.Value) writer.WriteLine($"{item.Key}={item.Value}");
            writer.WriteLine();
        }
    }
}

}
