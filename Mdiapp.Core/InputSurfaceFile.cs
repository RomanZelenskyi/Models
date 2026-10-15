using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mdiapp.Core
{
    public sealed class SurfaceCorrection
    {
        public double[] Arguments { get; private set; } = new double[1];
        public double[,] Values { get; private set; } = new double[2,1];
        public int BranchCount => Values.GetLength(0)/2;
        public void Resize(int branches,int points)
        {
            if(branches<1||branches>30||points<1||points>100)throw new InvalidDataException("Переходные коэффициенты: 1–30 ветвей, 1–100 точек.");
            if(branches==BranchCount&&points==Arguments.Length)return;
            var arguments=new double[points];Array.Copy(Arguments,arguments,Math.Min(points,Arguments.Length));var values=new double[branches*2,points];
            for(int row=0;row<Math.Min(values.GetLength(0),Values.GetLength(0));row++)for(int col=0;col<Math.Min(points,Arguments.Length);col++)values[row,col]=Values[row,col];Arguments=arguments;Values=values;
        }
    }
    // Units35/37: new multidimensional formats; old three-column results use Unit22.
    public sealed class InputSurfaceFile
    {
        public int Type { get; set; } = 42;
        public string Comment { get; set; } = "";
        public double[] Scales { get; } = Enumerable.Repeat(1d,6).ToArray();
        public double[] X { get; private set; } = new double[1];
        public double[] Y { get; private set; } = new double[1];
        public double[] Temperatures { get; private set; } = new double[1];
        public double[,] BranchValues { get; private set; } = new double[1,1];
        public double[,,,] Values { get; private set; } = new double[1,1,1,1];
        public SurfaceCorrection[] Corrections { get; } = {new SurfaceCorrection(),new SurfaceCorrection()};
        public List<string> TrailingLines { get; } = new List<string>();
        private readonly Dictionary<string,string> suffixes=new Dictionary<string,string>();
        public int BranchCount => BranchValues.GetLength(1);
        public bool IsUnit37 => new[]{44,47,50}.Contains(Type);
        public int ScaleCount => IsUnit37?4:6;
        public int CorrectionCount => IsUnit37?1:2;
        public void Resize(int x,int y,int temperatures,int branches)
        {
            if(x<1||x>100||y<1||y>100||temperatures<1||temperatures>30||branches<1||branches>50)throw new InvalidDataException("Допустимо: 1–100 точек по каждой оси, 1–30 температур и 1–50 ветвей.");
            if(IsUnit37&&branches!=1)throw new InvalidDataException("Формат Unit37 содержит одну таблицу для каждой температуры.");
            if(x==X.Length&&y==Y.Length&&temperatures==Temperatures.Length&&branches==BranchCount)return;
            var newX=new double[x];var newY=new double[y];var newT=new double[temperatures];var newB=new double[temperatures,branches];var newV=new double[temperatures,branches,y,x];
            Array.Copy(X,newX,Math.Min(x,X.Length));Array.Copy(Y,newY,Math.Min(y,Y.Length));Array.Copy(Temperatures,newT,Math.Min(temperatures,Temperatures.Length));
            for(int t=0;t<Math.Min(temperatures,Temperatures.Length);t++)for(int b=0;b<Math.Min(branches,BranchCount);b++)
            {newB[t,b]=BranchValues[t,b];for(int r=0;r<Math.Min(y,Y.Length);r++)for(int c=0;c<Math.Min(x,X.Length);c++)newV[t,b,r,c]=Values[t,b,r,c];}
            X=newX;Y=newY;Temperatures=newT;BranchValues=newB;Values=newV;
        }
        public static InputSurfaceFile Load(string path,CultureInfo culture=null,bool unit37=false)
        {
            culture=culture??CultureInfo.CurrentCulture;var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);int position=0;var model=new InputSurfaceFile();
            Func<string> next=()=>position<lines.Length?lines[position++]:throw new InvalidDataException("Неполный многомерный файл Unit"+(unit37?37:35)+".");
            string header=next().TrimStart();int split=header.IndexOfAny(new[]{' ','\t'});if(!int.TryParse(split<0?header:header.Substring(0,split),out int type)||!(unit37?new[]{44,47,50}:new[]{42,45,48}).Contains(type))throw new InvalidDataException(unit37?"Unit37 поддерживает многомерные файлы типов 44/47/50.":"Unit35 поддерживает многомерные файлы типов 42/45/48.");model.Type=type;model.Comment=split<0?"":header.Substring(split+1);
            Func<string,int,double[]> read=(key,count)=>
            {
                string line=next();var tokens=Regex.Matches(line,@"\S+");if(tokens.Count<count)throw new InvalidDataException("Недостаточно значений: "+key);
                var numbers=new double[count];for(int i=0;i<count;i++){string token=tokens[i].Value;if(!double.TryParse(token,NumberStyles.Float,culture,out numbers[i])&&!double.TryParse(token,NumberStyles.Float,CultureInfo.InvariantCulture,out numbers[i]))throw new InvalidDataException("Некорректное число: "+key);if(double.IsNaN(numbers[i])||double.IsInfinity(numbers[i]))throw new InvalidDataException("Число должно быть конечным.");}
                var last=tokens[count-1];model.suffixes[key]=line.Substring(last.Index+last.Length);return numbers;
            };
            Array.Copy(read("scales",model.ScaleCount),model.Scales,model.ScaleCount);var sizes=read("sizes",unit37?3:4);model.Resize(Size(sizes[0]),Size(sizes[1]),Size(sizes[2]),unit37?1:Size(sizes[3]));Array.Copy(read("x",model.X.Length),model.X,model.X.Length);Array.Copy(read("y",model.Y.Length),model.Y,model.Y.Length);
            for(int t=0;t<model.Temperatures.Length;t++)
            {model.Temperatures[t]=read("t"+t,1)[0];for(int b=0;b<model.BranchCount;b++){if(!unit37)model.BranchValues[t,b]=read("b"+t+"_"+b,1)[0];for(int row=0;row<model.Y.Length;row++){var data=read("v"+t+"_"+b+"_"+row,model.X.Length);for(int col=0;col<data.Length;col++)model.Values[t,b,row,col]=data[col];}}}
            for(int k=0;k<model.CorrectionCount;k++){string key="c"+k;var size=read(key+"sizes",2);var correction=model.Corrections[k];correction.Resize(Size(size[0]),Size(size[1]));Array.Copy(read(key+"x",correction.Arguments.Length),correction.Arguments,correction.Arguments.Length);for(int row=0;row<correction.BranchCount*2;row++){var data=read(key+"v"+row,correction.Arguments.Length);for(int col=0;col<data.Length;col++)correction.Values[row,col]=data[col];}}
            model.TrailingLines.AddRange(lines.Skip(position));return model;
        }
        private static int Size(double number){if(number!=Math.Truncate(number)||number<1||number>100)throw new InvalidDataException("Некорректный размер таблицы.");return (int)number;}
        public void Save(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;if(!new[]{42,44,45,47,48,50}.Contains(Type)||IsUnit37&&BranchCount!=1)throw new InvalidDataException("Недопустимый тип или число таблиц Unit35/37.");
            if(new[]{Comment}.Concat(TrailingLines).Any(s=>s.IndexOfAny(new[]{'\r','\n'})>=0))throw new InvalidDataException("Комментарий должен занимать одну строку.");
            var lines=new List<string>{Type+" "+Comment};Action<string,IEnumerable<double>> write=(key,data)=>
            {var fields=data.Select(value=>{if(double.IsNaN(value)||double.IsInfinity(value))throw new InvalidDataException("Значение должно быть конечным числом.");return value.ToString("G",culture);});lines.Add(string.Join("   ",fields)+(suffixes.TryGetValue(key,out string suffix)?suffix:""));};
            write("scales",Scales.Take(ScaleCount));write("sizes",(new double[]{X.Length,Y.Length,Temperatures.Length,BranchCount}).Take(IsUnit37?3:4));write("x",X);write("y",Y);
            for(int t=0;t<Temperatures.Length;t++){write("t"+t,new[]{Temperatures[t]});for(int b=0;b<BranchCount;b++){if(!IsUnit37)write("b"+t+"_"+b,new[]{BranchValues[t,b]});for(int row=0;row<Y.Length;row++){int r=row;write("v"+t+"_"+b+"_"+row,Enumerable.Range(0,X.Length).Select(col=>Values[t,b,r,col]));}}}
            for(int k=0;k<CorrectionCount;k++){string key="c"+k;var correction=Corrections[k];write(key+"sizes",new double[]{correction.BranchCount,correction.Arguments.Length});write(key+"x",correction.Arguments);for(int row=0;row<correction.BranchCount*2;row++){int r=row;write(key+"v"+row,Enumerable.Range(0,correction.Arguments.Length).Select(col=>correction.Values[r,col]));}}
            lines.AddRange(TrailingLines);string temporary=path+".new-"+Guid.NewGuid().ToString("N");try{File.WriteAllText(temporary,string.Join("\r\n",lines)+"\r\n",LegacyCodec.TextEncoding);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
        public static double[][] ParseClipboard(string text,CultureInfo culture=null)
        {
            var lines=(text??"").Replace("\r\n","\n").TrimEnd('\n').Split('\n');if(lines.Length>100)throw new InvalidDataException("Вставка превышает 100 строк.");return lines.Select(line=>{var fields=line.Split('\t');if(fields.Length>100)throw new InvalidDataException("Вставка превышает 100 столбцов.");return fields.Select(field=>InputFiles3132.Parse(field,culture??CultureInfo.CurrentCulture)).ToArray();}).ToArray();
        }
    }
}
