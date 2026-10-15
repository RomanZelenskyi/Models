using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mdiapp.Core
{
    public sealed class InputFile55
    {
        // Delphi writes grids 16/17/18 before grid 15; retain this file order.
        public static IReadOnlyList<int> GridOrder { get; } = Array.AsReadOnly(new[]{1,2,3,6,7,8,10,11,12,13,16,17,18,15,19});
        public string Comment { get; set; } = "";
        public InputCurveFile[] Curves { get; } = Enumerable.Range(0,15).Select(i=>new InputCurveFile()).ToArray();
        public List<string> TrailingLines { get; } = new List<string>();
        private readonly string[,] suffixes=new string[15,4];
        public static InputFile55 Load(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);if(lines.Length<61)throw new InvalidDataException("Файл 55 должен содержать пятнадцать характеристик.");
            string header=lines[0].TrimStart();int split=header.IndexOfAny(new[]{' ','\t'});if((split<0?header:header.Substring(0,split))!="55")throw new InvalidDataException("Unit38 поддерживает файл типа 55.");var model=new InputFile55{Comment=split<0?"":header.Substring(split+1)};
            for(int block=0;block<15;block++)
            {
                var curve=model.Curves[block];Func<int,int,double[]> read=(row,count)=>
                {
                    string line=lines[1+block*4+row];var tokens=Regex.Matches(line,@"\S+");if(tokens.Count<count)throw new InvalidDataException("Недостаточно значений характеристики "+(block+1)+".");var data=new double[count];
                    for(int i=0;i<count;i++)if((!double.TryParse(tokens[i].Value,NumberStyles.Float,culture,out data[i])&&!double.TryParse(tokens[i].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out data[i]))||double.IsNaN(data[i])||double.IsInfinity(data[i]))throw new InvalidDataException("Некорректное число в характеристике "+(block+1)+".");
                    var last=tokens[count-1];model.suffixes[block,row]=line.Substring(last.Index+last.Length);return data;
                };
                double parameter=read(0,1)[0],countValue=read(1,1)[0];if(parameter!=Math.Truncate(parameter)||parameter<0||parameter>2000||countValue!=Math.Truncate(countValue)||countValue<1||countValue>100)throw new InvalidDataException("Номер параметра: 0–2000; число точек: 1–100.");curve.Parameter=(int)parameter;curve.Count=(int)countValue;
                for(int row=0;row<2;row++){var values=read(row+2,curve.Count);for(int col=0;col<values.Length;col++)curve.Values[row,col]=values[col];}
                Func<int,string> caption=row=>{string suffix=model.suffixes[block,row];int p=suffix.IndexOf('!');return p<0?"":suffix.Substring(p+1);};curve.ParameterCaption=caption(0);curve.CountCaption=caption(1);curve.XCaption=caption(2);curve.YCaption=caption(3);
            }
            model.TrailingLines.AddRange(lines.Skip(61));return model;
        }
        public void Save(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;if(new[]{Comment}.Concat(TrailingLines).Any(s=>s.IndexOfAny(new[]{'\r','\n'})>=0))throw new InvalidDataException("Комментарий должен занимать одну строку.");
            var lines=new List<string>{"55 "+Comment};
            for(int block=0;block<15;block++)
            {
                var curve=Curves[block];if(curve==null||curve.Count<1||curve.Count>100||curve.Parameter<0||curve.Parameter>2000)throw new InvalidDataException("Недопустимый размер или параметр характеристики "+(block+1)+".");
                string[] captions={curve.ParameterCaption,curve.CountCaption,curve.XCaption,curve.YCaption};if(captions.Any(s=>s.IndexOfAny(new[]{'\r','\n'})>=0))throw new InvalidDataException("Подпись должна занимать одну строку.");
                Func<int,string> suffix=row=>suffixes[block,row]??(string.IsNullOrEmpty(captions[row])?"":"     !"+captions[row]);lines.Add(curve.Parameter+suffix(0));lines.Add(curve.Count+suffix(1));
                for(int row=0;row<2;row++){int r=row;var values=Enumerable.Range(0,curve.Count).Select(col=>{double value=curve.Values[r,col];if(double.IsNaN(value)||double.IsInfinity(value))throw new InvalidDataException("Значение должно быть конечным числом.");return value.ToString("G",culture);});lines.Add(string.Join(" ",values)+suffix(row+2));}
            }
            lines.AddRange(TrailingLines);string temporary=path+".new-"+Guid.NewGuid().ToString("N");try{File.WriteAllText(temporary,string.Join("\r\n",lines)+"\r\n",LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
