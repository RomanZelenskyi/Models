using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    // Unit24: four curves, parameter precedes point count in each block.
    public sealed class InputQuadCurveFile
    {
        public string Comment { get; set; } = "";
        public InputCurveFile[] Curves { get; } = { new InputCurveFile(), new InputCurveFile(), new InputCurveFile(), new InputCurveFile() };
        public static InputQuadCurveFile Load(string path,CultureInfo culture=null)
        {
            var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);
            if(lines.Length<17)throw new InvalidDataException("Файл типа 36 должен содержать четыре характеристики.");
            string header=lines[0].TrimStart();int split=header.IndexOf(' ');
            if((split<0?header:header.Substring(0,split))!="36")throw new InvalidDataException("Unit24 поддерживает файл типа 36.");
            var model=new InputQuadCurveFile{Comment=split<0?"":header.Substring(split+1)};
            for(int i=0;i<4;i++)
            {
                int offset=1+i*4;
                model.Curves[i]=InputCurveFile.ParseLines(new[]{"52 ",lines[offset+1],lines[offset],lines[offset+2],lines[offset+3]},culture);
            }
            return model;
        }
        public void Save(string path,CultureInfo culture=null)
        {
            if(Comment.IndexOfAny(new[]{'\r','\n'})>=0)throw new InvalidDataException("Комментарий должен занимать одну строку.");
            var lines=new[]{"36 "+Comment}.Concat(Curves.SelectMany(curve=>{var block=curve.SerializeLines(culture);return new[]{block[2],block[1],block[3],block[4]};})).ToArray();
            string temporary=path+".new-"+Guid.NewGuid().ToString("N");
            try{File.WriteAllText(temporary,string.Join("\r\n",lines)+"\r\n",LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
