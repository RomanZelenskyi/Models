using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mdiapp.Core
{
    public sealed class InputFile57
    {
        public string Comment { get; set; } = "";
        public double[] Constants { get; } = {1,1,1};
        public string ConstantsSuffix { get; set; } = "";
        public InputCurveFile[] Curves { get; } = {new InputCurveFile(),new InputCurveFile()};
        public List<string> TrailingLines { get; } = new List<string>();
        public static InputFile57 Load(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);if(lines.Length<10)throw new InvalidDataException("Файл 57 должен содержать три исходных значения и две характеристики.");string header=lines[0].TrimStart();int split=header.IndexOfAny(new[]{' ','\t'});if((split<0?header:header.Substring(0,split))!="57")throw new InvalidDataException("Unit40 поддерживает файл типа 57.");var model=new InputFile57{Comment=split<0?"":header.Substring(split+1)};
            var tokens=Regex.Matches(lines[1],@"\S+");if(tokens.Count<3)throw new InvalidDataException("Ожидаются три исходных значения.");for(int i=0;i<3;i++)if((!double.TryParse(tokens[i].Value,NumberStyles.Float,culture,out model.Constants[i])&&!double.TryParse(tokens[i].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out model.Constants[i]))||double.IsNaN(model.Constants[i])||double.IsInfinity(model.Constants[i]))throw new InvalidDataException("Некорректное исходное значение.");var last=tokens[2];model.ConstantsSuffix=lines[1].Substring(last.Index+last.Length);
            for(int i=0;i<2;i++){model.Curves[i]=InputCurveFile.ParseLines(new[]{"52 "}.Concat(lines.Skip(2+i*4).Take(4)).ToArray(),culture);for(int row=0;row<2;row++){string data=lines[4+i*4+row].Split('!')[0];if(data.Length<model.Curves[i].Count*14&&data.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries).Length<model.Curves[i].Count)throw new InvalidDataException("Неполный ряд характеристики.");}}
            model.TrailingLines.AddRange(lines.Skip(10));return model;
        }
        public void Save(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;if(new[]{Comment,ConstantsSuffix}.Concat(TrailingLines).Any(s=>s.IndexOfAny(new[]{'\r','\n'})>=0))throw new InvalidDataException("Комментарий должен занимать одну строку.");
            var constants=Constants.Select(value=>{if(double.IsNaN(value)||double.IsInfinity(value))throw new InvalidDataException("Исходное значение должно быть конечным числом.");return (value==-1000?1:value).ToString("G",culture);});
            var lines=new List<string>{"57 "+Comment,"  "+string.Join("  ",constants)+ConstantsSuffix};foreach(var curve in Curves)lines.AddRange(curve.SerializeLines(culture).Skip(1));lines.AddRange(TrailingLines);
            string temporary=path+".new-"+Guid.NewGuid().ToString("N");try{File.WriteAllText(temporary,string.Join("\r\n",lines)+"\r\n",LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
