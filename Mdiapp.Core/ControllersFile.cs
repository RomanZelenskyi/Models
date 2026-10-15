using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    public sealed class ControllerComponent
    {
        public int Mode { get; set; }
        public double Constant { get; set; }
        public InputCurveFile Table { get; } = new InputCurveFile();
    }
    public sealed class ControllerSetting
    {
        public int Parameter { get; set; }
        public string Comment { get; set; } = "";
        public int Mode { get; set; }
        public ControllerComponent[] Components { get; } = Enumerable.Range(0,5).Select(i=>new ControllerComponent()).ToArray();
        public double Kp { get; set; }
        public double Ki { get; set; }
        public override string ToString() => Parameter+" "+Comment;
    }
    public sealed class ControllersFile
    {
        public int Type { get; set; } = 40;
        public string Comment { get; set; } = "";
        public List<ControllerSetting>[,] Groups { get; } = new List<ControllerSetting>[3,5];
        public string[,] GroupCaptions { get; } = new string[3,5];
        public double[] AdditionalValues { get; } = new double[3];
        public bool HasGains { get; set; }
        public bool HasAdditionalValues { get; set; }
        public string[] TrailingLines { get; set; } = new string[0];
        public ControllersFile(){for(int c=0;c<3;c++)for(int g=0;g<5;g++){Groups[c,g]=new List<ControllerSetting>();GroupCaptions[c,g]="";}}
        private sealed class Reader
        {
            private readonly string[] lines; public int Position; public bool End=>Position>=lines.Length;
            public Reader(string[] source){lines=source;}
            public string Next(){if(End)throw new InvalidDataException("Неполный файл регуляторов.");return lines[Position++];}
            public string[] Remaining()=>lines.Skip(Position).ToArray();
        }
        private static string Data(string line)=>line.Split('!')[0].Trim();
        private static string Caption(string line){int i=line.IndexOf('!');return i<0?"":line.Substring(i+1);}
        private static string[] Tokens(string line)=>Data(line).Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
        private static int Integer(string line,int min,int max){if(!int.TryParse(Tokens(line).FirstOrDefault(),NumberStyles.Integer,CultureInfo.InvariantCulture,out int value)||value<min||value>max)throw new InvalidDataException("Недопустимый код или количество в файле регуляторов.");return value;}
        private static void ReadComponent(Reader reader,ControllerComponent component,int mode,CultureInfo culture)
        {
            component.Mode=mode;
            if(mode==0)component.Constant=InputFiles3132.Parse(Tokens(reader.Next()).FirstOrDefault()??"",culture);
            else if(mode==2)component.Table.Parameter=Integer(reader.Next(),0,2000);
            else
            {
                string count=reader.Next();component.Table.Count=Integer(count,1,30);component.Table.CountCaption=Caption(count);
                string argument=reader.Next();component.Table.Parameter=Integer(argument,0,2000);component.Table.ParameterCaption=Caption(argument);
                for(int row=0;row<2;row++)
                {
                    string line=reader.Next();var values=Tokens(line);if(values.Length<component.Table.Count)throw new InvalidDataException("Недостаточно значений таблицы регулятора.");
                    for(int i=0;i<component.Table.Count;i++)component.Table.Values[row,i]=InputFiles3132.Parse(values[i],culture);
                    if(row==0)component.Table.XCaption=Caption(line);else component.Table.YCaption=Caption(line);
                }
            }
        }
        public static ControllersFile Load(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;var reader=new Reader(File.ReadAllLines(path,LegacyCodec.TextEncoding));string header=reader.Next().TrimStart();int split=header.IndexOf(' ');
            int type=Integer(split<0?header:header.Substring(0,split),29,40);if(type!=29&&type!=40)throw new InvalidDataException("Unit25 поддерживает типы 29 и 40.");
            var model=new ControllersFile{Type=type,Comment=split<0?"":header.Substring(split+1)};
            for(int category=0;category<3;category++)for(int group=0;group<5;group++)
            {
                string countLine=reader.Next();int count=Integer(countLine,0,category==0?20:5);model.GroupCaptions[category,group]=Caption(countLine);
                for(int i=0;i<count;i++)
                {
                    string parameter=reader.Next();var item=new ControllerSetting{Parameter=Integer(parameter,0,2000),Comment=Caption(parameter),Mode=Integer(reader.Next(),0,category==0?3:2)};
                    if(item.Mode<2)ReadComponent(reader,item.Components[0],item.Mode,culture);
                    else for(int part=0;part<(item.Mode==3?5:3);part++)ReadComponent(reader,item.Components[part],Integer(reader.Next(),0,2),culture);
                    model.Groups[category,group].Add(item);
                }
            }
            if(!reader.End)
            {
                model.HasGains=true;
                for(int category=0;category<3;category++)for(int group=0;group<5;group++)
                {
                    int count=Integer(reader.Next(),0,category==0?20:5);if(count!=model.Groups[category,group].Count)throw new InvalidDataException("Количество коэффициентов не совпадает с количеством регуляторов.");
                    foreach(var item in model.Groups[category,group]){var values=Tokens(reader.Next());if(values.Length<2)throw new InvalidDataException("Нет Kp/Ki.");item.Kp=InputFiles3132.Parse(values[0],culture);item.Ki=InputFiles3132.Parse(values[1],culture);}
                }
            }
            if(!reader.End)
            {
                model.HasAdditionalValues=true;var first=Tokens(reader.Next());var second=Tokens(reader.Next());if(first.Length<1||second.Length<2)throw new InvalidDataException("Нет дополнительных значений регуляторов.");
                model.AdditionalValues[0]=InputFiles3132.Parse(first[0],culture);model.AdditionalValues[1]=InputFiles3132.Parse(second[0],culture);model.AdditionalValues[2]=InputFiles3132.Parse(second[1],culture);
            }
            model.TrailingLines=reader.Remaining();return model;
        }
        private static string Number(double value,CultureInfo culture){if(double.IsNaN(value)||double.IsInfinity(value))throw new InvalidDataException("Значение должно быть конечным числом.");return value.ToString("G",culture);}
        private static void OneLine(string value){if((value??"").IndexOfAny(new[]{'\r','\n'})>=0)throw new InvalidDataException("Комментарий должен занимать одну строку.");}
        private static void WriteComponent(List<string> lines,ControllerComponent component,int mode,CultureInfo culture)
        {
            if(mode==0)lines.Add(Number(component.Constant,culture));
            else
            {
                if(component.Table.Parameter<0||component.Table.Parameter>2000)throw new InvalidDataException("Номер аргумента: 0–2000.");
                if(mode==2)lines.Add(component.Table.Parameter.ToString());
                else
                {
                    if(component.Table.Count<1||component.Table.Count>30)throw new InvalidDataException("Таблица регулятора: 1–30 точек.");
                    OneLine(component.Table.CountCaption);OneLine(component.Table.ParameterCaption);OneLine(component.Table.XCaption);OneLine(component.Table.YCaption);
                    lines.Add(component.Table.Count+"         !"+component.Table.CountCaption);lines.Add(component.Table.Parameter+"         !"+component.Table.ParameterCaption);
                    for(int row=0;row<2;row++)lines.Add(string.Join("  ",Enumerable.Range(0,component.Table.Count).Select(i=>Number(component.Table.Values[row,i],culture)))+"   !"+(row==0?component.Table.XCaption:component.Table.YCaption));
                }
            }
        }
        public void Save(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;OneLine(Comment);if(Type!=29&&Type!=40)throw new InvalidDataException("Тип файла: 29 или 40.");var lines=new List<string>{Type+" "+Comment};
            for(int category=0;category<3;category++)for(int group=0;group<5;group++)
            {
                var items=Groups[category,group];if(items.Count>(category==0?20:5))throw new InvalidDataException("Основных регуляторов до 20, дополнительных до 5.");OneLine(GroupCaptions[category,group]);lines.Add(items.Count+"         !"+GroupCaptions[category,group]);
                foreach(var item in items)
                {
                    OneLine(item.Comment);if(item.Parameter<0||item.Parameter>2000||item.Mode<0||item.Mode>(category==0?3:2))throw new InvalidDataException("Недопустимый параметр или режим регулятора.");
                    lines.Add(item.Parameter+"         !"+item.Comment);lines.Add(item.Mode.ToString());
                    if(item.Mode<2)WriteComponent(lines,item.Components[0],item.Mode,culture);
                    else for(int part=0;part<(item.Mode==3?5:3);part++){int mode=item.Components[part].Mode;if(mode<0||mode>2)throw new InvalidDataException("Недопустимый вид компонента.");lines.Add(mode.ToString());WriteComponent(lines,item.Components[part],mode,culture);}
                }
            }
            bool gains=HasGains||HasAdditionalValues||AdditionalValues.Any(v=>v!=0)||Groups.Cast<List<ControllerSetting>>().SelectMany(g=>g).Any(c=>c.Kp!=0||c.Ki!=0);
            if(gains)for(int category=0;category<3;category++)for(int group=0;group<5;group++){lines.Add(Groups[category,group].Count+"         !"+GroupCaptions[category,group]);foreach(var item in Groups[category,group])lines.Add(Number(item.Kp,culture)+"  "+Number(item.Ki,culture));}
            if(HasAdditionalValues||AdditionalValues.Any(v=>v!=0)){lines.Add(Number(AdditionalValues[0],culture));lines.Add(Number(AdditionalValues[1],culture)+"  "+Number(AdditionalValues[2],culture));}
            lines.AddRange(TrailingLines);
            string temporary=path+".new-"+Guid.NewGuid().ToString("N");try{File.WriteAllText(temporary,string.Join("\r\n",lines)+"\r\n",LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
