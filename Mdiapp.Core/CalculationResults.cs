using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Mdiapp.Core
{
    public sealed class CalculationResults
    {
        public int[] ParameterIds { get; }
        public List<double[]> Rows { get; } = new List<double[]>();
        public CalculationResults(int[] ids){if(ids.Length<1||ids.Length>800)throw new InvalidDataException("Результаты: 1–800 параметров.");ParameterIds=(int[])ids.Clone();}
        public static CalculationResults LoadParameters(string path,int[] ids)
        {
            var result=new CalculationResults(ids);var row=new List<double>();foreach(string line in File.ReadLines(path,LegacyCodec.TextEncoding))foreach(double value in ProjectDocument.Numbers(line)){row.Add(value);if(row.Count==ids.Length){result.Rows.Add(row.ToArray());row.Clear();if(result.Rows.Count>10000)throw new InvalidDataException("Результаты превышают 10000 режимов.");}}if(row.Count!=0)throw new InvalidDataException("Последний режим param.dat неполный.");if(result.Rows.Count==0)throw new InvalidDataException("В param.dat нет результатов.");return result;
        }
        public static CalculationResults LoadNative(string path)
        {
            using(var reader=new StreamReader(path,LegacyCodec.TextEncoding)){var result=new CalculationResults(ProjectDocument.Integers(reader.ReadLine()??""));string line;while((line=reader.ReadLine())!=null){if(string.IsNullOrWhiteSpace(line))continue;var row=ProjectDocument.Numbers(line);if(row.Length!=result.ParameterIds.Length)throw new InvalidDataException("Неполная строка RES.");result.Rows.Add(row);if(result.Rows.Count>10000)throw new InvalidDataException("Более 10000 режимов RES.");}return result;}
        }
        public string Export(ResultFileFormat format,ResultExportOption option,Func<int,string> caption,string input="")
        {
            int digits=Math.Max(0,Math.Min(15,option.Digits));Func<double,string> number=v=>v.ToString(format==ResultFileFormat.Native?"G17":option.FormatIndex==0?"E"+digits:option.FormatIndex==1?"F"+digits:"G"+Math.Max(1,digits),CultureInfo.InvariantCulture);
            var table=new List<string[]>();if(format==ResultFileFormat.Native){table.Add(ParameterIds.Select(i=>i.ToString(CultureInfo.InvariantCulture)).ToArray());table.AddRange(Rows.Select(row=>row.Select(number).ToArray()));}
            else if(option.Transpose){for(int col=0;col<ParameterIds.Length;col++){int c=col;table.Add(new[]{caption(ParameterIds[c])}.Concat(Rows.Select(row=>number(row[c]))).ToArray());}}
            else{table.Add(ParameterIds.Select(caption).ToArray());table.AddRange(Rows.Select(row=>row.Select(number).ToArray()));}
            if(format==ResultFileFormat.Html){var html=new StringBuilder("<!doctype html><html><head><meta charset=\"utf-8\"><title>MDIAPP — результаты</title></head><body>");if(option.IncludeInput)html.Append("<pre>").Append(WebUtility.HtmlEncode(input)).Append("</pre>");html.Append("<table border=\"1\">");foreach(var row in table){html.Append("<tr>");foreach(string field in row)html.Append("<td>").Append(WebUtility.HtmlEncode(field)).Append("</td>");html.Append("</tr>");}html.Append("</table>");if(option.IncludeGraphs&&ParameterIds.Length>1&&Rows.Count>0){double minX=Rows.Min(r=>r[0]),dx=Rows.Max(r=>r[0])-minX;for(int c=1;c<ParameterIds.Length;c++){int col=c;double minY=Rows.Min(r=>r[col]),dy=Rows.Max(r=>r[col])-minY;html.Append("<h3>").Append(WebUtility.HtmlEncode(caption(ParameterIds[col]))).Append("</h3><svg width=\"800\" height=\"260\" viewBox=\"0 0 800 260\"><polyline fill=\"none\" stroke=\"blue\" points=\"");foreach(var row in Rows)html.Append((20+(row[0]-minX)/(dx==0?1:dx)*760).ToString("F2",CultureInfo.InvariantCulture)).Append(',').Append((240-(row[col]-minY)/(dy==0?1:dy)*220).ToString("F2",CultureInfo.InvariantCulture)).Append(' ');html.Append("\"/></svg>");}}return html.Append("</body></html>").ToString();}
            return (format!=ResultFileFormat.Native&&option.IncludeInput?input+"\r\n":"")+string.Join("\r\n",table.Select(row=>string.Join(format==ResultFileFormat.Native?" ":"\t",row)))+"\r\n";
        }
        public void Save(string path,ResultFileFormat format,ResultExportOption option,Func<int,string> caption,string input="")
        {
            string text=Export(format,option,caption,input),temporary=path+".new-"+Guid.NewGuid().ToString("N");try{File.WriteAllText(temporary,text,format==ResultFileFormat.Html?new UTF8Encoding(false):LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
