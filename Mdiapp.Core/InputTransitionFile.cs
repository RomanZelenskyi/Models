using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mdiapp.Core
{
    // Unit36: new 43/46/49 format. The old three-column format belongs to Unit23.
    public sealed class InputTransitionFile
    {
        public int Type { get; set; } = 43;
        public string Comment { get; set; } = "";
        public double Scale { get; set; } = 1;
        public string ScaleSuffix { get; set; } = "";
        public InputCurveFile Curve { get; } = new InputCurveFile();
        public SurfaceCorrection Correction { get; } = new SurfaceCorrection();
        public List<string> TrailingLines { get; } = new List<string>();
        private readonly Dictionary<string,string> suffixes=new Dictionary<string,string>();
        public static InputTransitionFile Load(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);int position=0;var model=new InputTransitionFile();
            Func<string> next=()=>position<lines.Length?lines[position++]:throw new InvalidDataException("Неполный файл Unit36.");
            string header=next().TrimStart();int split=header.IndexOfAny(new[]{' ','\t'});if(!int.TryParse(split<0?header:header.Substring(0,split),out int type)||!new[]{43,46,49}.Contains(type))throw new InvalidDataException("Unit36 поддерживает файлы типов 43/46/49.");model.Type=type;model.Comment=split<0?"":header.Substring(split+1);
            Func<string,int,bool,double[]> read=(key,count,fixedFields)=>
            {
                string line=next();var tokens=Regex.Matches(line,@"\S+");var values=new double[count];bool valid=tokens.Count>=count;
                if(valid)for(int i=0;i<count;i++)if(!TryNumber(tokens[i].Value,culture,out values[i])){valid=false;break;}
                int end;if(valid){var last=tokens[count-1];end=last.Index+last.Length;}
                else if(fixedFields&&line.Length>=count*14){for(int i=0;i<count;i++)if(!TryNumber(line.Substring(i*14,14).Trim(),culture,out values[i]))throw new InvalidDataException("Некорректное число: "+key);end=count*14;}
                else throw new InvalidDataException("Некорректная или неполная строка: "+key);
                model.suffixes[key]=line.Substring(end);return values;
            };
            model.Scale=read("scale",1,false)[0];model.ScaleSuffix=model.suffixes["scale"];
            model.Curve.Count=Size(read("count",1,false)[0],100);
            for(int row=0;row<2;row++){var data=read("curve"+row,model.Curve.Count,true);for(int col=0;col<data.Length;col++)model.Curve.Values[row,col]=data[col];}
            var sizes=read("sizes",2,false);model.Correction.Resize(Size(sizes[0],30),Size(sizes[1],100));var arguments=read("argument",model.Correction.Arguments.Length,true);Array.Copy(arguments,model.Correction.Arguments,arguments.Length);
            for(int row=0;row<model.Correction.BranchCount*2;row++){var data=read("transition"+row,arguments.Length,true);for(int col=0;col<data.Length;col++)model.Correction.Values[row,col]=data[col];}
            model.TrailingLines.AddRange(lines.Skip(position));return model;
        }
        private static bool TryNumber(string text,CultureInfo culture,out double value)=>
            (double.TryParse(text,NumberStyles.Float,culture,out value)||double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out value))&&!double.IsNaN(value)&&!double.IsInfinity(value);
        private static int Size(double value,int maximum){if(value!=Math.Truncate(value)||value<1||value>maximum)throw new InvalidDataException("Количество должно быть от 1 до "+maximum+".");return (int)value;}
        public void Save(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;if(!new[]{43,46,49}.Contains(Type)||Curve.Count<1||Curve.Count>100)throw new InvalidDataException("Недопустимый тип или число точек Unit36.");
            if(new[]{Comment,ScaleSuffix}.Concat(TrailingLines).Any(s=>s.IndexOfAny(new[]{'\r','\n'})>=0))throw new InvalidDataException("Комментарий должен занимать одну строку.");
            Func<double,bool,string> format=(value,fixedField)=>{if(double.IsNaN(value)||double.IsInfinity(value))throw new InvalidDataException("Значение должно быть конечным числом.");string field=value.ToString(fixedField?"F6":"G",culture);if(fixedField&&field.Length>14)throw new InvalidDataException("Значение не помещается в поле из 14 символов.");return fixedField?field.PadLeft(14):field;};
            var lines=new List<string>{Type+" "+Comment,format(Scale,false)+ScaleSuffix};Action<string,IEnumerable<double>,bool> write=(key,values,fixedFields)=>lines.Add(string.Join(fixedFields?"":"   ",values.Select(value=>format(value,fixedFields)))+(suffixes.TryGetValue(key,out string suffix)?suffix:""));
            write("count",new double[]{Curve.Count},false);for(int row=0;row<2;row++){int r=row;write("curve"+row,Enumerable.Range(0,Curve.Count).Select(col=>Curve.Values[r,col]),true);}
            write("sizes",new double[]{Correction.BranchCount,Correction.Arguments.Length},false);write("argument",Correction.Arguments,true);
            for(int row=0;row<Correction.BranchCount*2;row++){int r=row;write("transition"+row,Enumerable.Range(0,Correction.Arguments.Length).Select(col=>Correction.Values[r,col]),true);}lines.AddRange(TrailingLines);
            string temporary=path+".new-"+Guid.NewGuid().ToString("N");try{File.WriteAllText(temporary,string.Join("\r\n",lines)+"\r\n",LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
