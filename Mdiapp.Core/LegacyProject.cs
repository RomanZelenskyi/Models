using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace Mdiapp.Core
{

// ChildWin.savedatafile: retain all unported sections byte-for-byte.
public sealed class LegacyProject
{
    private readonly byte[] original;
    private readonly int remarkEnd;
    private readonly string originalRemark;
    public string Remark { get; set; }
    public string FileCode { get; }
    public int ProjectKey { get; }
    public IReadOnlyList<string> IncludedFiles { get; }
    private LegacyProject(byte[] bytes)
    {
        original = bytes;
        remarkEnd = Array.IndexOf(bytes, (byte)10);
        if (remarkEnd < 0) throw new InvalidDataException("Отсутствует заголовок INM.");
        using var reader = new StringReader(LegacyCodec.TextEncoding.GetString(bytes));
        Remark = originalRemark = reader.ReadLine() ?? "";
        FileCode = reader.ReadLine() ?? throw new InvalidDataException("Отсутствует код файла.");
        if (!int.TryParse(reader.ReadLine(), out var key)) throw new InvalidDataException("Недопустимый ключ проекта.");
        ProjectKey = key;
        if (!int.TryParse(reader.ReadLine(), out var count) || (count < 0 || count > 2000))
            throw new InvalidDataException("Недопустимое число подключённых файлов.");
        var files = new List<string>();
        for (int i = 0; i < count; i++) files.Add(reader.ReadLine() ?? throw new InvalidDataException("Неполный список файлов."));
        IncludedFiles = files.AsReadOnly();
    }
    public static LegacyProject Load(string path) => new LegacyProject(File.ReadAllBytes(path));
    public void Save(string path)
    {
        if (Remark == originalRemark) { File.WriteAllBytes(path, original); return; }
        if (Remark.IndexOf('\r') >= 0 || Remark.IndexOf('\n') >= 0) throw new InvalidDataException("Комментарий должен занимать одну строку.");
        using var output = new MemoryStream();
        var header = LegacyCodec.TextEncoding.GetBytes(Remark); output.Write(header, 0, header.Length);
        int offset = remarkEnd > 0 && original[remarkEnd - 1] == 13 ? remarkEnd - 1 : remarkEnd; output.Write(original, offset, original.Length - offset);
        File.WriteAllBytes(path, output.ToArray());
    }
}

}
