using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mdiapp.Core
{
    public sealed class InputFile56
    {
        public string Comment { get; set; } = "";
        public int[] Modes { get; } = {1,1};
        // Inactive modes are session drafts: the file stores only the selected mode.
        public double[,,] Coefficients { get; } = new double[2,2,4];
        public double[,] AdditionalValues { get; } = new double[2,4];
        public List<string> TrailingLines { get; } = new List<string>();
        private readonly string[,] suffixes=new string[2,3];
        public static InputFile56 Load(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);if(lines.Length<7)throw new InvalidDataException("Файл 56 должен содержать две группы параметров.");string header=lines[0].TrimStart();int split=header.IndexOfAny(new[]{' ','\t'});if((split<0?header:header.Substring(0,split))!="56")throw new InvalidDataException("Unit39 поддерживает файл типа 56.");var model=new InputFile56{Comment=split<0?"":header.Substring(split+1)};
            for(int group=0;group<2;group++)
            {
                Func<int,int,double[]> read=(row,count)=>{string line=lines[1+group*3+row];var tokens=Regex.Matches(line,@"\S+");if(tokens.Count<count)throw new InvalidDataException("Недостаточно значений в файле 56.");var values=new double[count];for(int i=0;i<count;i++)if((!double.TryParse(tokens[i].Value,NumberStyles.Float,culture,out values[i])&&!double.TryParse(tokens[i].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out values[i]))||double.IsNaN(values[i])||double.IsInfinity(values[i]))throw new InvalidDataException("Некорректное число в файле 56.");var last=tokens[count-1];model.suffixes[group,row]=line.Substring(last.Index+last.Length);return values;};
                double mode=read(0,1)[0];if(mode!=1&&mode!=2)throw new InvalidDataException("Режим файла 56 должен быть 1 или 2.");model.Modes[group]=(int)mode;var coefficients=read(1,4);var extra=read(2,4);for(int i=0;i<4;i++){model.Coefficients[group,(int)mode-1,i]=coefficients[i];model.AdditionalValues[group,i]=extra[i];}
            }
            model.TrailingLines.AddRange(lines.Skip(7));return model;
        }
        public void Save(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;if(new[]{Comment}.Concat(TrailingLines).Any(s=>s.IndexOfAny(new[]{'\r','\n'})>=0))throw new InvalidDataException("Комментарий должен занимать одну строку.");var lines=new List<string>{"56 "+Comment};
            Func<double,string> format=value=>{if(double.IsNaN(value)||double.IsInfinity(value))throw new InvalidDataException("Значение должно быть конечным числом.");return value.ToString("G",culture);};
            for(int group=0;group<2;group++){int mode=Modes[group];if(mode!=1&&mode!=2)throw new InvalidDataException("Режим файла 56 должен быть 1 или 2.");lines.Add(mode+(suffixes[group,0]??""));int g=group;lines.Add(string.Join("  ",Enumerable.Range(0,4).Select(i=>format(Coefficients[g,mode-1,i])))+(suffixes[group,1]??""));lines.Add(string.Join("  ",Enumerable.Range(0,4).Select(i=>format(AdditionalValues[g,i])))+(suffixes[group,2]??""));}
            lines.AddRange(TrailingLines);string temporary=path+".new-"+Guid.NewGuid().ToString("N");try{File.WriteAllText(temporary,string.Join("\r\n",lines)+"\r\n",LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
