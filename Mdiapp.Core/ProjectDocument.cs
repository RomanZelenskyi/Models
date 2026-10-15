using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    public sealed class ProjectDocument
    {
        public string Remark { get; set; } = "";
        public string FileCode { get; set; } = "";
        public int ProjectKey { get; set; }
        public List<string> IncludedFiles { get; } = new List<string>();
        public int[] Options { get; } = new int[13];
        public int DeclaredControlCount { get; set; }
        public int[] ArgumentIds { get; set; } = Enumerable.Repeat(-1,12).ToArray();
        public List<double[]> OperatingPoints { get; } = new List<double[]>();
        public double[] Tolerances { get; set; } = {1e-5,1e-6,.1};
        public List<int> ResultIds { get; } = new List<int>();
        public double[] IdentificationSteps { get; set; } = {-.01,-.01,-.01};
        public int[] DynamicFactors { get; set; } = new int[0];
        public int[] ControlIds { get; set; } = new int[0];
        public double[] ControlValues { get; set; } = new double[0];
        public int[] NodeIds { get; set; } = new int[0];
        public double[] NodeValues { get; set; } = new double[0];
        public int[] CorrectionIds { get; set; } = new int[0];
        public double[][] CorrectionValues { get; set; } = {new double[0],new double[0],new double[0]};
        public int CorrectionRows { get; set; } = 3;
        public List<string> TrailingLines { get; } = new List<string>();
        private byte[] original;
        private string snapshot;
        public static double[] Numbers(string line,int width=0)
        {
            string data=line.Split('!')[0];var tokens=data.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
            if(width>0&&tokens.Any(t=>t.Length>width)&&data.Length%width==0)tokens=Enumerable.Range(0,data.Length/width).Select(i=>data.Substring(i*width,width).Trim()).Where(t=>t.Length>0).ToArray();
            return tokens.Select(t=>{if(!double.TryParse(t,NumberStyles.Float,CultureInfo.InvariantCulture,out double v)&&!double.TryParse(t,NumberStyles.Float,CultureInfo.CurrentCulture,out v))throw new InvalidDataException("Некорректное число INM: "+t);if(double.IsNaN(v)||double.IsInfinity(v))throw new InvalidDataException("Неконечное число INM.");return v;}).ToArray();
        }
        public static int[] Integers(string line,int width=0)=>Numbers(line,width).Select(v=>{if(v!=Math.Truncate(v)||v<int.MinValue||v>int.MaxValue)throw new InvalidDataException("Ожидается целое число INM.");return (int)v;}).ToArray();
        public static ProjectDocument Load(string path)
        {
            return FromBytes(File.ReadAllBytes(path));
        }
        public static ProjectDocument FromBytes(byte[] bytes)
        {
            var text=LegacyCodec.TextEncoding.GetString(bytes).Replace("\r\n","\n");var lines=text.EndsWith("\n",StringComparison.Ordinal)?text.Substring(0,text.Length-1).Split('\n'):text.Split('\n');int position=0;
            Func<string> next=()=>position<lines.Length?lines[position++]:throw new InvalidDataException("Неполный проект INM.");
            Func<int[]> ints=()=>Integers(next(),4);var model=new ProjectDocument{Remark=next(),FileCode=next()};model.ProjectKey=One(ints());int files=One(ints());if(files<0||files>2000)throw new InvalidDataException("Недопустимое число файлов INM.");for(int i=0;i<files;i++)model.IncludedFiles.Add(next());
            var options=ints();if(options.Length!=13)throw new InvalidDataException("В INM ожидаются задача, конфигурация, условия и десять флагов.");Array.Copy(options,model.Options,13);var counts=ints();if(counts.Length!=5||counts.Any(n=>n<0)||counts[0]>10000||counts.Skip(1).Any(n=>n>2000))throw new InvalidDataException("Недопустимые размеры INM.");
            int columns=One(ints());if(columns<1||columns>30)throw new InvalidDataException("Число аргументов INM: 1–30.");model.ArgumentIds=Integers(next(),15);if(model.ArgumentIds.Length!=columns)throw new InvalidDataException("Неверное число идентификаторов аргументов.");for(int i=0;i<counts[0];i++){var row=Numbers(next());if(row.Length!=columns)throw new InvalidDataException("Неполная точка режима INM.");model.OperatingPoints.Add(row);}
            model.Tolerances=Numbers(next());model.DeclaredControlCount=counts[4];while(model.ResultIds.Count<counts[1]){var ids=ints();if(ids.Length==0||model.ResultIds.Count+ids.Length>counts[1])throw new InvalidDataException("Неполный список результатов INM.");model.ResultIds.AddRange(ids);}
            if(model.Options[0]==7){model.IdentificationSteps=Numbers(next());model.DynamicFactors=ints();model.ControlIds=Integers(next(),15);model.ControlValues=Numbers(next());if(model.ControlIds.Length!=counts[4]||model.ControlValues.Length!=counts[4])throw new InvalidDataException("Неверное число управляющих параметров.");}
            if(model.Options[0]==5)model.DynamicFactors=ints();
            if(counts[2]>0){model.NodeIds=Integers(next(),15);model.NodeValues=Numbers(next());if(model.NodeIds.Length!=counts[2]||model.NodeValues.Length!=counts[2])throw new InvalidDataException("Неполный блок поправок узлов.");}
            if(counts[3]>0){model.CorrectionIds=Integers(next(),15);model.CorrectionRows=0;for(int row=0;row<3;row++){if(position<lines.Length&&Numbers(lines[position]).Length==counts[3]){model.CorrectionValues[row]=Numbers(next());model.CorrectionRows++;}else model.CorrectionValues[row]=new double[counts[3]];}if(model.CorrectionIds.Length!=counts[3]||model.CorrectionRows==0)throw new InvalidDataException("Неполный блок многопараметрических поправок.");}
            model.TrailingLines.AddRange(lines.Skip(position));model.snapshot=model.Serialize();model.original=bytes;return model;
        }
        private static int One(int[] numbers){if(numbers.Length!=1)throw new InvalidDataException("Ожидается одно целое число INM.");return numbers[0];}
        public static ProjectDocument Create(Unit3Model catalog)
        {
            var model=new ProjectDocument{ProjectKey=catalog.EngineJobs.GetInt("configs","key",1),Remark="Новый проект"};model.Options[0]=catalog.Jobs.FirstOrDefault(j=>j.Enabled)?.Number??1;model.Options[1]=catalog.Configurations.FirstOrDefault(c=>c.Enabled)?.Number??1;model.Options[2]=catalog.Conditions.FirstOrDefault(c=>c.Enabled)?.Number??0;model.ArgumentIds[0]=catalog.TimeParameter;string prefix="n"+model.Options[2];int conditions=catalog.GlobalJobs.GetInt("Conditions",prefix+"_count",0);for(int i=0;i<Math.Min(conditions,10);i++)model.ArgumentIds[i+1]=catalog.GlobalJobs.GetInt("Conditions",prefix+"_p"+(i+1),-1);if(conditions<10&&catalog.SelectedControls.Count>0)model.ArgumentIds[conditions+1]=catalog.SelectedControls.Min();model.OperatingPoints.Add(model.ArgumentIds.Select(id=>id<0?-1d:0d).ToArray());model.ResultIds.AddRange(catalog.SelectedParameters.Where(i=>i!=catalog.TimeParameter).OrderBy(i=>i));return model;
        }
        private static string IntFields(IEnumerable<int> values,int width)=>string.Concat(values.Select(v=>{string s=v.ToString(CultureInfo.InvariantCulture);if(s.Length>width)throw new InvalidDataException("Идентификатор не помещается в поле INM.");return s.PadLeft(width);}));
        private static string FloatFields(IEnumerable<double> values)=>string.Concat(values.Select(v=>{if(double.IsNaN(v)||double.IsInfinity(v))throw new InvalidDataException("Значение должно быть конечным.");string s=v.ToString("F6",CultureInfo.InvariantCulture);if(s.Length>15)throw new InvalidDataException("Значение не помещается в поле INM.");return s.PadLeft(15);}));
        public string Serialize(bool includeResults=true)
        {
            if(OperatingPoints.Count<1||OperatingPoints.Count>10000||ArgumentIds.Length<1||ArgumentIds.Length>30||OperatingPoints.Any(r=>r.Length!=ArgumentIds.Length)||NodeIds.Length!=NodeValues.Length||ControlIds.Length!=ControlValues.Length||CorrectionValues.Length!=3||CorrectionValues.Any(r=>r.Length!=CorrectionIds.Length))throw new InvalidDataException("Несогласованные размеры проекта.");
            if(ArgumentIds.Any(i=>i< -1||i>2000)||ResultIds.Count>799||ResultIds.Distinct().Count()!=ResultIds.Count||ResultIds.Any(i=>i<1||i>2000)||ResultIds.Contains(ArgumentIds[0])||NodeIds.Distinct().Count()!=NodeIds.Length||CorrectionRows<1||CorrectionRows>3)throw new InvalidDataException("Недопустимые или повторяющиеся идентификаторы проекта.");
            if(new[]{Remark,FileCode}.Concat(IncludedFiles).Concat(TrailingLines).Any(s=>s.IndexOfAny(new[]{'\r','\n'})>=0))throw new InvalidDataException("Строка проекта содержит перевод строки.");
            var lines=new List<string>{Remark,FileCode,IntFields(new[]{ProjectKey},4),IntFields(new[]{IncludedFiles.Count},4)};lines.AddRange(IncludedFiles);lines.Add(IntFields(Options,4));lines.Add(IntFields(new[]{OperatingPoints.Count,ResultIds.Count,NodeIds.Length,CorrectionIds.Length,Options[0]==7?ControlIds.Length:DeclaredControlCount},4));lines.Add(IntFields(new[]{ArgumentIds.Length},4));lines.Add(IntFields(ArgumentIds,15));lines.AddRange(OperatingPoints.Select(FloatFields));lines.Add(string.Concat(Tolerances.Select(v=>{if(double.IsNaN(v)||double.IsInfinity(v))throw new InvalidDataException("Недопустимая точность.");return v.ToString("0.00000E+000",CultureInfo.InvariantCulture).PadLeft(14);})));for(int i=0;i<ResultIds.Count;i+=20)lines.Add(IntFields(ResultIds.Skip(i).Take(20),4));
            if(Options[0]==7){lines.Add(FloatFields(IdentificationSteps));lines.Add(IntFields(DynamicFactors,4));lines.Add(IntFields(ControlIds,15));lines.Add(FloatFields(ControlValues));}if(Options[0]==5)lines.Add(IntFields(DynamicFactors,4));if(NodeIds.Length>0){lines.Add(IntFields(NodeIds,15));lines.Add(FloatFields(NodeValues));}if(CorrectionIds.Length>0){lines.Add(IntFields(CorrectionIds,15));int rows=CorrectionRows;for(int i=rows;i<3;i++)if(CorrectionValues[i].Any(v=>v!=0))rows=i+1;lines.AddRange(CorrectionValues.Take(rows).Select(FloatFields));}if(includeResults)lines.AddRange(TrailingLines);return string.Join("\r\n",lines)+"\r\n";
        }
        public byte[] Bytes(){string text=Serialize();return original!=null&&text==snapshot?(byte[])original.Clone():LegacyCodec.TextEncoding.GetBytes(text);}
        public void Save(string path)
        {
            byte[] bytes=Bytes();string temporary=path+".new-"+Guid.NewGuid().ToString("N");try{File.WriteAllBytes(temporary,bytes);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
