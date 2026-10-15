using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    public sealed class MultiCharacteristicBranch
    {
        public double Value { get; set; }
        public InputCurveFile Curve { get; } = new InputCurveFile();
        public override string ToString()=>Value.ToString(CultureInfo.CurrentCulture);
    }
    public sealed class MultiCharacteristic
    {
        public double Mach { get; set; }
        public List<MultiCharacteristicBranch> Branches { get; } = new List<MultiCharacteristicBranch>();
        public override string ToString()=>"M = "+Mach.ToString(CultureInfo.CurrentCulture);
    }
    public sealed class MultiCharacteristicFile
    {
        public string Comment { get; set; } = "";
        public List<MultiCharacteristic> Characteristics { get; } = new List<MultiCharacteristic>();
        private static string[] Tokens(string line)=>line.Split('!')[0].Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
        private static int Count(string value,int maximum){if(!int.TryParse(value,out int count)||count<1||count>maximum)throw new InvalidDataException("Недопустимое количество характеристик, ветвей или точек.");return count;}
        public static MultiCharacteristicFile Load(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);int position=0;
            Func<string> next=()=>position<lines.Length?lines[position++]:throw new InvalidDataException("Неполный файл типа 1500.");
            string header=next().TrimStart();int split=header.IndexOf(' ');if((split<0?header:header.Substring(0,split))!="1500")throw new InvalidDataException("Unit26 поддерживает файл типа 1500.");
            var model=new MultiCharacteristicFile{Comment=split<0?"":header.Substring(split+1)};var counts=Tokens(next());int count=Count(counts.FirstOrDefault(),30);
            for(int i=0;i<count;i++)
            {
                string title=next();string dataTitle=title.Split('!')[0];string[] fields=Tokens(title);
                if(dataTitle.Length>=19&&dataTitle.IndexOf('\t')<0)fields=new[]{dataTitle.Substring(0,3).Trim(),dataTitle.Substring(3,4).Trim(),dataTitle.Substring(7,12).Trim()};
                if(fields.Length<3)throw new InvalidDataException("Нет размеров или числа Маха характеристики.");int branches=Count(fields[0],30),maximum=Count(fields[1],100);var item=new MultiCharacteristic{Mach=InputFiles3132.Parse(fields[2],culture)};
                for(int b=0;b<branches;b++)
                {
                    string branchTitle=next();string branchData=branchTitle.Split('!')[0];fields=Tokens(branchTitle);
                    if(branchData.Length>=16&&branchData.IndexOf('\t')<0)fields=new[]{branchData.Substring(0,12).Trim(),branchData.Substring(12,4).Trim()};
                    if(fields.Length<2)throw new InvalidDataException("Нет значения или числа точек ветви.");var branch=new MultiCharacteristicBranch{Value=InputFiles3132.Parse(fields[0],culture)};branch.Curve.Count=Count(fields[1],100);if(branch.Curve.Count>maximum)throw new InvalidDataException("Число точек ветви превышает размер характеристики.");
                    for(int row=0;row<2;row++)
                    {
                        string line=next();string data=line.Split('!')[0];var tokens=Tokens(line);int comment=line.IndexOf('!');string caption=comment<0?"":line.Substring(comment+1);if(row==0)branch.Curve.XCaption=caption;else branch.Curve.YCaption=caption;
                        bool fixedWidth=data.Length>=branch.Curve.Count*12&&data.IndexOf('\t')<0;
                        if(!fixedWidth&&tokens.Length<branch.Curve.Count)throw new InvalidDataException("Недостаточно точек ветви.");
                        for(int p=0;p<branch.Curve.Count;p++)branch.Curve.Values[row,p]=InputFiles3132.Parse(fixedWidth?data.Substring(p*12,12).Trim():tokens[p],culture);
                    }
                    item.Branches.Add(branch);
                }
                model.Characteristics.Add(item);
            }
            if(lines.Skip(position).Any(s=>!string.IsNullOrWhiteSpace(s)))throw new InvalidDataException("Лишние данные после характеристик.");return model;
        }
        private static string Format(double value,CultureInfo culture){if(double.IsNaN(value)||double.IsInfinity(value))throw new InvalidDataException("Значение должно быть конечным числом.");string text=value.ToString("F6",culture);if(text.Length>12)throw new InvalidDataException("Значение не помещается в поле из 12 символов.");return text.PadLeft(12);}
        public void Save(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;if(Comment.IndexOfAny(new[]{'\r','\n'})>=0)throw new InvalidDataException("Комментарий должен занимать одну строку.");
            if(Characteristics.Count<1||Characteristics.Count>30)throw new InvalidDataException("Характеристик: 1–30.");var lines=new List<string>{"1500 "+Comment,Characteristics.Count.ToString().PadLeft(3)+"               !Количество характеристик"};
            foreach(var item in Characteristics)
            {
                if(item.Branches.Count<1||item.Branches.Count>30||item.Branches.Any(b=>b.Curve.Count<1||b.Curve.Count>100))throw new InvalidDataException("Ветвей: 1–30; точек: 1–100.");
                lines.Add(item.Branches.Count.ToString().PadLeft(3)+item.Branches.Max(b=>b.Curve.Count).ToString().PadLeft(4)+Format(item.Mach,culture)+"               !Ветви, точки, M");
                foreach(var branch in item.Branches)
                {
                    lines.Add(Format(branch.Value,culture)+branch.Curve.Count.ToString().PadLeft(4)+"               !Значение ветви, точки");
                    for(int row=0;row<2;row++)
                    {
                        string caption=row==0?branch.Curve.XCaption:branch.Curve.YCaption;if(caption.IndexOfAny(new[]{'\r','\n'})>=0)throw new InvalidDataException("Подпись должна занимать одну строку.");
                        lines.Add(string.Concat(Enumerable.Range(0,branch.Curve.Count).Select(p=>Format(branch.Curve.Values[row,p],culture)))+"          !"+caption);
                    }
                }
            }
            string temporary=path+".new-"+Guid.NewGuid().ToString("N");try{File.WriteAllText(temporary,string.Join("\r\n",lines)+"\r\n",LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
