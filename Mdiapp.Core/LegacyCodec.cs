using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
namespace Mdiapp.Core
{

// Unit41: little-endian signed Int32 records, -777 terminates each line.
public static class LegacyCodec
{
    private static readonly int[] Key = new int[] {1,5,2,7,2,3,4,6,9,8,1,2,9,5,4,7,3,2,11,8};
    public static Encoding TextEncoding { get; } = CreateEncoding();
    private static Encoding CreateEncoding()
    {
        
        return Encoding.GetEncoding(1251);
    }
    public static void Encode(Stream text, Stream coded)
    {
        using var reader = new StreamReader(text, TextEncoding, false, 4096, true);
        using var writer = new BinaryWriter(coded, TextEncoding, true);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            var bytes = TextEncoding.GetBytes(line);
            for (int i = 0; i < bytes.Length; i++) writer.Write(bytes[i] + Key[i % Key.Length]);
            writer.Write(-777);
        }
    }
    public static void Decode(Stream coded, Stream text)
    {
        using var reader = new BinaryReader(coded, TextEncoding, true);
        var line = new List<byte>();
        while (true)
        {
            int first = coded.ReadByte();
            if (first < 0) break;
            var tail = reader.ReadBytes(3);
            if (tail.Length != 3) throw new InvalidDataException("Неполная запись Int32 в COD.");
            int value = first | tail[0] << 8 | tail[1] << 16 | tail[2] << 24;
            if (value == -777)
            {
                var bytes = line.ToArray(); text.Write(bytes, 0, bytes.Length); text.Write(new byte[] {13, 10}, 0, 2); line.Clear();
            }
            else
            {
                int decoded = value - Key[line.Count % Key.Length];
                if (decoded < 0 || decoded > 255) throw new InvalidDataException("Недопустимый символ в COD.");
                line.Add((byte)decoded);
            }
        }
        if (line.Count != 0) throw new InvalidDataException("Отсутствует конец строки в COD.");
    }
}

}
