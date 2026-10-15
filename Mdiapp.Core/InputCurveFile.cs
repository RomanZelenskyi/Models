using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    // Units17/18: paired X/Y rows, original file type retained.
    public sealed class InputCurveFile
    {
        public int Type { get; set; } = 52;
        public string Comment { get; set; } = "";
        public int Count { get; set; } = 1;
        public int Parameter { get; set; }
        public double[,] Values { get; } = new double[2,100];
        public string CountSuffix { get; set; } = "";
        public string CountCaption { get; set; } = "";
        public string ParameterCaption { get; set; } = "";
        public string XCaption { get; set; } = "";
        public string YCaption { get; set; } = "";
        private static string BeforeComment(string s) => s.Split('!')[0];
        private static string Caption(string s) { int p=s.IndexOf('!');return p<0?"":s.Substring(p+1); }
        private static int Integer(string s) => int.TryParse(s,NumberStyles.Integer,CultureInfo.InvariantCulture,out int value)?value:throw new InvalidDataException("Некорректное число точек или номер параметра.");
        public static bool IsSupportedType(int type) => new[]{1,52,5,8,18,26,11,16,20}.Contains(type);
        public static InputCurveFile Load(string path,CultureInfo culture=null)
        {
            return ParseLines(File.ReadAllLines(path,LegacyCodec.TextEncoding),culture);
        }
        internal static InputCurveFile ParseLines(string[] lines,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;
            if(lines.Length<5)throw new InvalidDataException("Файл характеристики должен содержать пять строк.");
            string header=lines[0].TrimStart();int space=header.IndexOf(' ');
            var model=new InputCurveFile{Type=Integer(space<0?header:header.Substring(0,space)),Comment=space<0?"":header.Substring(space+1)};
            if(!IsSupportedType(model.Type))throw new InvalidDataException("Недопустимый тип двухрядной характеристики.");
            string count=BeforeComment(lines[1]).Trim();int split=count.IndexOfAny(new[]{' ','\t'});
            model.Count=Integer(split<0?count:count.Substring(0,split));model.CountSuffix=split<0?"":count.Substring(split);
            model.Parameter=Integer(BeforeComment(lines[2]).Trim());
            if(model.Count<1||model.Count>100||model.Parameter<0||model.Parameter>2000)throw new InvalidDataException("Точек: 1–100; номер параметра: 0–2000.");
            model.CountCaption=Caption(lines[1]);model.ParameterCaption=Caption(lines[2]);model.XCaption=Caption(lines[3]);model.YCaption=Caption(lines[4]);
            for(int row=0;row<2;row++)
            {
                string data=BeforeComment(lines[row+3]);var tokens=data.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
                for(int col=0;col<model.Count;col++)
                {
                    string field=data.Length>=model.Count*14?data.Substring(col*14,14).Trim():col<tokens.Length?tokens[col]:"";
                    model.Values[row,col]=InputFiles3132.Parse(field,culture);
                }
            }
            return model;
        }
        internal string[] SerializeLines(CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;
            if(!IsSupportedType(Type)||Count<1||Count>100||Parameter<0||Parameter>2000)throw new InvalidDataException("Недопустимый тип, число точек или параметр.");
            if(new[]{Comment,CountSuffix,CountCaption,ParameterCaption,XCaption,YCaption}.Any(s=>s.IndexOfAny(new[]{'\r','\n'})>=0))throw new InvalidDataException("Комментарии должны занимать одну строку.");
            var lines=new[]{Type+" "+Comment,Count+CountSuffix+"          !"+CountCaption,Parameter+"          !"+ParameterCaption,"",""};
            for(int row=0;row<2;row++)
            {
                for(int col=0;col<Count;col++)
                {
                    double value=Values[row,col];if(double.IsNaN(value)||double.IsInfinity(value))throw new InvalidDataException("Значение должно быть конечным числом.");
                    string field=value.ToString("F6",culture);if(field.Length>14)throw new InvalidDataException("Значение не помещается в поле из 14 символов.");lines[row+3]+=field.PadLeft(14);
                }
                lines[row+3]+="     !"+(row==0?XCaption:YCaption);
            }
            return lines;
        }
        public void Save(string path,CultureInfo culture=null)
        {
            var lines=SerializeLines(culture);
            string temporary=path+".new-"+Guid.NewGuid().ToString("N");
            try{File.WriteAllText(temporary,string.Join("\r\n",lines)+"\r\n",LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
        public static double[][] ParseClipboardRows(string text,CultureInfo culture=null) => CharacteristicFile.ParseClipboardRows(text,culture).Take(2).ToArray();
    }
}
