using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    // Unit20: three independent four-line X/Y blocks.
    public sealed class InputTripleCurveFile
    {
        public int Type { get; set; } = 21;
        public string Comment { get; set; } = "";
        public InputCurveFile[] Curves { get; } = { new InputCurveFile(), new InputCurveFile(), new InputCurveFile() };
        public static InputTripleCurveFile Load(string path,CultureInfo culture=null)
        {
            var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);
            if(lines.Length<13)throw new InvalidDataException("Файл должен содержать заголовок и три характеристики.");
            string header=lines[0].TrimStart();int split=header.IndexOf(' ');
            if(!int.TryParse(split<0?header:header.Substring(0,split),NumberStyles.Integer,CultureInfo.InvariantCulture,out int type)||!new[]{21,22,27}.Contains(type))throw new InvalidDataException("Unit20 поддерживает типы 21, 22 и 27.");
            var model=new InputTripleCurveFile{Type=type,Comment=split<0?"":header.Substring(split+1)};
            for(int i=0;i<3;i++)model.Curves[i]=InputCurveFile.ParseLines(new[]{"52 "}.Concat(lines.Skip(1+i*4).Take(4)).ToArray(),culture);
            return model;
        }
        public void Save(string path,CultureInfo culture=null)
        {
            if(!new[]{21,22,27}.Contains(Type))throw new InvalidDataException("Unit20 поддерживает типы 21, 22 и 27.");
            if(Comment.IndexOfAny(new[]{'\r','\n'})>=0)throw new InvalidDataException("Комментарий должен занимать одну строку.");
            var lines=new[]{Type+" "+Comment}.Concat(Curves.SelectMany(curve=>curve.SerializeLines(culture).Skip(1))).ToArray();
            string temporary=path+".new-"+Guid.NewGuid().ToString("N");
            try{File.WriteAllText(temporary,string.Join("\r\n",lines)+"\r\n",LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
