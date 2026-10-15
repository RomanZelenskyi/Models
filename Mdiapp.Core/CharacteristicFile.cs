using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    public sealed class CharacteristicBranch
    {
        public double Value { get; set; }
        public double[,] Points { get; set; } = new double[3,0];
    }
    // Unit16: characteristics stored as groups of three lines per ten points.
    public sealed class CharacteristicFile
    {
        public int Type { get; set; }
        public string Comment { get; set; } = "";
        public double[] Coefficients { get; } = new double[4];
        public int Interpolation { get; set; }
        public double AdditionalValue { get; set; }
        public string SpecialSuffix { get; set; } = "";
        public string ControlSuffix { get; set; } = "";
        public bool IsSpecial => Type == 15 || Type == 17 || Type == 19;
        public List<CharacteristicBranch> Branches { get; } = new List<CharacteristicBranch>();
        public static double[][] ParseClipboardRows(string text,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;
            var rows=(text??"").Replace("\r\n","\n").TrimEnd('\n').Split('\n').Take(3).Select(line=>line.Split('\t')).ToArray();
            if(rows.Any(row=>row.Length>100))throw new InvalidDataException("Вставка превышает размер таблицы.");
            return rows.Select(row=>row.Select(cell=>InputFiles3132.Parse(cell,culture)).ToArray()).ToArray();
        }
        private static string Field(string s,int offset,int width) => offset >= s.Length ? "" : s.Substring(offset,Math.Min(width,s.Length-offset)).Trim();
        private static int Integer(string s) => int.TryParse(s,NumberStyles.Integer,CultureInfo.InvariantCulture,out int value) ? value : throw new InvalidDataException("Некорректное число ветвей или точек.");
        public static CharacteristicFile Load(string path,CultureInfo culture=null,bool extended=false)
        {
            culture=culture??CultureInfo.CurrentCulture;
            var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding); if(lines.Length<4)throw new InvalidDataException("Неполный файл характеристики.");
            string header=lines[0].TrimStart();int split=header.IndexOf(' ');
            int type=Integer(split<0?header:header.Substring(0,split));if(type<1||type>2000)throw new InvalidDataException("Недопустимый тип характеристики.");
            var model=new CharacteristicFile{Type=type,Comment=split<0?"":header.Substring(split+1)};
            var coefficients=lines[1].Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
            for(int i=0;i<4;i++)model.Coefficients[i]=InputFiles3132.Parse(i<coefficients.Length?coefficients[i]:"",culture);
            model.Interpolation=Integer(Field(lines[2],0,4));
            if(model.IsSpecial)model.SpecialSuffix=lines[2].Length>4?lines[2].Substring(4):"";
            else { model.AdditionalValue=InputFiles3132.Parse(Field(lines[2],4,6),culture); model.ControlSuffix=lines[2].Length>10?lines[2].Substring(10):""; }
            int branches=Integer(Field(lines[3],0,3));int maximum=Integer(Field(lines[3],3,3));
            if(branches<0||branches>30||maximum<0||maximum>(extended?100:99))throw new InvalidDataException("Недопустимое количество ветвей или точек.");
            int line=4;
            for(int branch=0;branch<branches;branch++)
            {
                if(line>=lines.Length)throw new InvalidDataException("Нет заголовка ветви.");
                string title=lines[line++];var parts=title.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
                int count=Integer(parts.Length==2?parts[1]:title.Length>7?title.Substring(7).Trim():"");
                if(count<1||count>(extended?100:99)||count>maximum)throw new InvalidDataException("Недопустимое число точек ветви.");
                var item=new CharacteristicBranch{Value=InputFiles3132.Parse(parts.Length==2?parts[0]:Field(title,0,7),culture),Points=new double[3,count]};
                for(int start=0;start<count;start+=10)for(int row=0;row<3;row++)
                {
                    if(line>=lines.Length)throw new InvalidDataException("Неполные данные ветви.");
                    string values=lines[line++];for(int col=0;col<Math.Min(10,count-start);col++)item.Points[row,start+col]=InputFiles3132.Parse(Field(values,col*7,7),culture);
                }
                model.Branches.Add(item);
            }
            return model;
        }
        private static string Format(double value,int width,int digits,CultureInfo culture)
        {
            if(double.IsNaN(value)||double.IsInfinity(value))throw new InvalidDataException("Значение должно быть конечным числом.");
            string result=value.ToString("F"+digits,culture);if(result.Length>width)throw new InvalidDataException("Значение не помещается в поле из "+width+" символов.");return result.PadLeft(width);
        }
        public void Save(string path,CultureInfo culture=null,bool extended=false)
        {
            culture=culture??CultureInfo.CurrentCulture;
            if(Type<1||Type>2000||Branches.Count>30||Branches.Any(b=>b.Points.GetLength(0)!=3||b.Points.GetLength(1)<1||b.Points.GetLength(1)>(extended?100:99)))throw new InvalidDataException("Недопустимое количество ветвей или точек.");
            if(new[]{Comment,SpecialSuffix,ControlSuffix}.Any(s=>s.IndexOfAny(new[]{'\r','\n'})>=0))throw new InvalidDataException("Комментарий должен занимать одну строку.");
            if(!new[]{0,5,-5,10,-10}.Contains(Interpolation))throw new InvalidDataException("Недопустимый режим интерполяции.");
            if(Coefficients.Any(v=>double.IsNaN(v)||double.IsInfinity(v)))throw new InvalidDataException("Коэффициенты должны быть конечными числами.");
            var lines=new List<string>{Type+" "+Comment,string.Join(" ",Coefficients.Select(v=>v.ToString("G",culture))),Format(Interpolation,4,0,culture)+(IsSpecial?SpecialSuffix:Format(AdditionalValue,6,1,culture)+ControlSuffix),Branches.Count.ToString().PadLeft(3)+(Branches.Count==0?0:Branches.Max(b=>b.Points.GetLength(1))).ToString().PadLeft(3)};
            foreach(var branch in Branches)
            {
                if(double.IsNaN(branch.Value)||double.IsInfinity(branch.Value))throw new InvalidDataException("Значение ветви должно быть конечным числом.");
                int count=branch.Points.GetLength(1);lines.Add(extended?" "+branch.Value.ToString("G",culture)+"  "+count:Format(branch.Value,7,1,culture)+count.ToString().PadLeft(2));
                for(int start=0;start<count;start+=10)for(int row=0;row<3;row++)lines.Add(string.Concat(Enumerable.Range(start,Math.Min(10,count-start)).Select(col=>Format(branch.Points[row,col],7,row==0?3:4,culture))));
            }
            string temporary=path+".new-"+Guid.NewGuid().ToString("N");
            try{File.WriteAllText(temporary,string.Join("\r\n",lines)+"\r\n",LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
