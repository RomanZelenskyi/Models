using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    // Unit34: fourteen curve blocks with scalar records interleaved after blocks 4/5/8/9.
    public sealed class InputFile300
    {
        public string Comment { get; set; } = "";
        public double[][] InitialValues { get; } = {new double[3],new double[3]};
        public string[] InitialCaptions { get; } = {"",""};
        public InputCurveFile[] Curves { get; } = Enumerable.Range(0,14).Select(i=>new InputCurveFile()).ToArray();
        public double[][] Limits { get; } = {new double[8],new double[8]};
        public double[] Constants { get; } = new double[2];
        public double[][] AdditionalValues { get; } = {new double[3],new double[3],new double[4],new double[4]};
        public int AdditionalLineCount { get; set; }
        public List<string> TrailingLines { get; } = new List<string>();
        public static InputFile300 Load(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);int position=0;
            Func<string> next=()=>position<lines.Length?lines[position++]:throw new InvalidDataException("Неполный файл 300.");
            string header=next().TrimStart();int split=header.IndexOf(' ');
            if((split<0?header:header.Substring(0,split))!="300")throw new InvalidDataException("Unit34 поддерживает файл типа 300.");
            var model=new InputFile300{Comment=split<0?"":header.Substring(split+1)};
            for(int row=0;row<2;row++){string line=next();ReadValues(line,model.InitialValues[row],0,3,9,culture);int p=line.IndexOf('!');model.InitialCaptions[row]=p<0?"":line.Substring(p+1);}
            for(int i=0;i<14;i++)
            {
                model.Curves[i]=InputCurveFile.ParseLines(new[]{"52 ",next(),next(),next(),next()},culture);
                if(i==3||i==7){int group=i==3?0:1;for(int row=0;row<4;row++)ReadValues(next(),model.Limits[group],row*2,2,14,culture);}
                if(i==4||i==8){var value=new double[1];ReadValues(next(),value,0,1,0,culture);model.Constants[i==4?0:1]=value[0];}
            }
            for(int i=0;i<4&&position<lines.Length&&!string.IsNullOrWhiteSpace(lines[position]);i++){ReadValues(next(),model.AdditionalValues[i],0,model.AdditionalValues[i].Length,0,culture);model.AdditionalLineCount++;}
            model.TrailingLines.AddRange(lines.Skip(position));return model;
        }
        private static void ReadValues(string line,double[] values,int offset,int count,int width,CultureInfo culture)
        {
            string data=line.Split('!')[0];var tokens=data.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
            bool fixedFields=width>0&&data.Length>=count*width;
            if(!fixedFields&&tokens.Length!=count)throw new InvalidDataException("Неверное число значений в файле 300.");
            for(int i=0;i<count;i++)values[offset+i]=InputFiles3132.Parse(fixedFields?data.Substring(i*width,width).Trim():tokens[i],culture);
        }
        public void Save(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;
            Func<IEnumerable<double>,int,int,string> format=(values,width,precision)=>string.Join(width==0?"  ":"",values.Select(value=>
            {
                if(double.IsNaN(value)||double.IsInfinity(value))throw new InvalidDataException("Значение должно быть конечным числом.");
                string text=value.ToString(width==0?"G":"F"+precision,culture);if(width>0&&text.Length>width)throw new InvalidDataException("Значение не помещается в поле файла 300.");return width==0?text:text.PadLeft(width);
            }));
            if(new[]{Comment}.Concat(InitialCaptions).Concat(TrailingLines).Any(s=>s.IndexOfAny(new[]{'\r','\n'})>=0))throw new InvalidDataException("Комментарий должен занимать одну строку.");
            var lines=new List<string>{"300 "+Comment};for(int i=0;i<2;i++)lines.Add(format(InitialValues[i],9,3)+"     !"+InitialCaptions[i]);
            for(int i=0;i<14;i++)
            {
                lines.AddRange(Curves[i].SerializeLines(culture).Skip(1));
                if(i==3||i==7)for(int row=0;row<4;row++)lines.Add(format(Limits[i==3?0:1].Skip(row*2).Take(2),14,6));
                if(i==4||i==8)lines.Add(format(new[]{Constants[i==4?0:1]},0,0));
            }
            if(AdditionalLineCount<0||AdditionalLineCount>4)throw new InvalidDataException("Неверное число дополнительных строк.");
            int count=AdditionalLineCount;for(int i=0;i<4;i++)if(AdditionalValues[i].Any(v=>v!=0))count=Math.Max(count,i+1);
            for(int i=0;i<count;i++)lines.Add(format(AdditionalValues[i],0,0));lines.AddRange(TrailingLines);
            string temporary=path+".new-"+Guid.NewGuid().ToString("N");
            try{File.WriteAllText(temporary,string.Join("\r\n",lines)+"\r\n",LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
