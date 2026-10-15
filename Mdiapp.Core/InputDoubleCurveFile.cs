using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    // Unit19: two independent four-line curve blocks plus three constants.
    public sealed class InputDoubleCurveFile
    {
        public string Comment { get; set; } = "";
        public InputCurveFile[] Curves { get; } = { new InputCurveFile(), new InputCurveFile() };
        public double[] Constants { get; } = new double[3];
        public string ConstantsCaption { get; set; } = "";
        public static InputDoubleCurveFile Load(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);
            if(lines.Length<10)throw new InvalidDataException("Файл типа 14 должен содержать две характеристики и строку констант.");
            string header=lines[0].TrimStart();int split=header.IndexOf(' ');
            if((split<0?header:header.Substring(0,split))!="14")throw new InvalidDataException("Unit19 поддерживает файл типа 14.");
            var model=new InputDoubleCurveFile{Comment=split<0?"":header.Substring(split+1)};
            for(int i=0;i<2;i++)model.Curves[i]=InputCurveFile.ParseLines(new[]{"52 "}.Concat(lines.Skip(1+i*4).Take(4)).ToArray(),culture);
            int comment=lines[9].IndexOf('!');string data=comment<0?lines[9]:lines[9].Substring(0,comment);model.ConstantsCaption=comment<0?"":lines[9].Substring(comment+1);
            var tokens=data.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
            for(int i=0;i<3;i++)model.Constants[i]=InputFiles3132.Parse(data.Length>=42?data.Substring(i*14,14).Trim():i<tokens.Length?tokens[i]:"",culture);
            return model;
        }
        public void Save(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;
            if(new[]{Comment,ConstantsCaption}.Any(s=>s.IndexOfAny(new[]{'\r','\n'})>=0))throw new InvalidDataException("Комментарий должен занимать одну строку.");
            var lines=new[]{"14 "+Comment}.Concat(Curves[0].SerializeLines(culture).Skip(1)).Concat(Curves[1].SerializeLines(culture).Skip(1)).ToList();
            string fields="";
            foreach(double value in Constants)
            {
                if(double.IsNaN(value)||double.IsInfinity(value))throw new InvalidDataException("Константы должны быть конечными числами.");
                string field=value.ToString("F3",culture);if(field.Length>14)throw new InvalidDataException("Константа не помещается в поле из 14 символов.");fields+=field.PadLeft(14);
            }
            lines.Add(fields+"     !"+ConstantsCaption);
            string temporary=path+".new-"+Guid.NewGuid().ToString("N");
            try{File.WriteAllText(temporary,string.Join("\r\n",lines)+"\r\n",LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
