using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    // Units22/23: one or three initial values and up to 1000 X/Y1/Y2 rows.
    public sealed class InputThreeColumnFile
    {
        public int Type { get; set; } = 42;
        public string Comment { get; set; } = "";
        public double[] InitialValues { get; } = new double[3];
        public double InitialValue { get => InitialValues[0]; set => InitialValues[0]=value; }
        public bool IsUnit23 => new[]{43,46,49}.Contains(Type);
        public List<double[]> Rows { get; } = new List<double[]>();
        public static bool Supports(int type) => new[]{42,43,44,45,46,47,48,49,50}.Contains(type);
        public static InputThreeColumnFile Load(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);
            if(lines.Length<2)throw new InvalidDataException("Файл должен содержать заголовок и начальное значение.");
            string header=lines[0].TrimStart();int split=header.IndexOf(' ');
            if(!int.TryParse(split<0?header:header.Substring(0,split),out int type)||!Supports(type))throw new InvalidDataException("Редактор поддерживает типы 42–50.");
            var model=new InputThreeColumnFile{Type=type,Comment=split<0?"":header.Substring(split+1)};
            var initial=lines[1].Split('!')[0].Trim().Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
            int initialCount=model.IsUnit23?3:1;
            if(initial.Length!=initialCount)throw new InvalidDataException("Ожидается "+initialCount+" исходных значений. Этот файл содержит другой формат результатов.");
            for(int i=0;i<initialCount;i++)model.InitialValues[i]=InputFiles3132.Parse(initial[i],culture);
            foreach(string line in lines.Skip(2).Where(s=>!string.IsNullOrWhiteSpace(s)))
            {
                string data=line.Split('!')[0];var tokens=data.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);var row=new double[3];
                if((data.Length<72||data.IndexOf('\t')>=0)&&tokens.Length>3)throw new InvalidDataException("В строке таблицы должно быть три значения.");
                if(data.Length>72&&data.IndexOf('\t')<0&&!string.IsNullOrWhiteSpace(data.Substring(72)))throw new InvalidDataException("Лишние данные после трёх столбцов.");
                for(int i=0;i<3;i++)row[i]=InputFiles3132.Parse(data.Length>=72&&data.IndexOf('\t')<0?data.Substring(i*24,24).Trim():i<tokens.Length?tokens[i]:"",culture);
                model.Rows.Add(row);if(model.Rows.Count>1000)throw new InvalidDataException("Допустимо не более 1000 строк.");
            }
            return model;
        }
        private static string Format(double value,int width,int digits,CultureInfo culture)
        {
            if(double.IsNaN(value)||double.IsInfinity(value))throw new InvalidDataException("Значение должно быть конечным числом.");
            string field=value.ToString("F"+digits,culture);if(field.Length>width)throw new InvalidDataException("Значение не помещается в поле из "+width+" символов.");return field.PadLeft(width);
        }
        public void Save(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;
            if(!Supports(Type)||Rows.Count<1||Rows.Count>1000||Rows.Any(r=>r==null||r.Length!=3))throw new InvalidDataException("Допустимы 1–1000 строк по три значения.");
            if(Comment.IndexOfAny(new[]{'\r','\n'})>=0)throw new InvalidDataException("Комментарий должен занимать одну строку.");
            if(InitialValues.Take(IsUnit23?3:1).Any(v=>double.IsNaN(v)||double.IsInfinity(v)))throw new InvalidDataException("Исходные значения должны быть конечными числами.");
            string initial=IsUnit23?string.Concat(InitialValues.Select(v=>"  "+v.ToString("G",culture))):Format(InitialValue,9,2,culture);
            var lines=new[]{Type+" "+Comment,initial}.Concat(Rows.Select(row=>string.Concat(row.Select(v=>Format(v,24,14,culture))))).ToArray();
            string temporary=path+".new-"+Guid.NewGuid().ToString("N");
            try{File.WriteAllText(temporary,string.Join("\r\n",lines)+"\r\n",LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
        public static double[][] ParseClipboardRows(string text,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;string[] lines=(text??"").Replace("\r\n","\n").TrimEnd('\n').Split('\n');
            if(lines.Length>1000)throw new InvalidDataException("Вставка превышает 1000 строк.");
            return lines.Select(line=>{var fields=line.Split('\t');if(fields.Length>3)throw new InvalidDataException("Вставка должна содержать не более трёх столбцов.");return fields.Select(v=>InputFiles3132.Parse(v,culture)).ToArray();}).ToArray();
        }
    }
}
