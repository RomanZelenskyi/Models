using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit40Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit40Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"57.dat");var model=new InputFile57{Comment="Проверка",ConstantsSuffix="   ! initial values"};model.Constants[0]=1.25e-5;model.Constants[1]=293.125;model.Constants[2]=110.5;
                for(int curve=0;curve<2;curve++){model.Curves[curve].Count=curve==0?7:100;model.Curves[curve].Parameter=curve==0?0:1999;for(int point=0;point<model.Curves[curve].Count;point++){model.Curves[curve].Values[0,point]=point+.125;model.Curves[curve].Values[1,point]=curve+point+.75;}}model.TrailingLines.Add("! tail");model.Save(path,CultureInfo.InvariantCulture);
                Check(Equal(model,InputFile57.Load(path,CultureInfo.InvariantCulture)),"Unit40 independent curves 100 points unknown and zero parameters");model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(Equal(model,InputFile57.Load(path,CultureInfo.GetCultureInfo("ru-RU")))&&File.Exists(path+".bak"),"Unit40 comma culture and backup");
                model.Constants[2]=-1000;model.Save(path);Check(InputFile57.Load(path).Constants[2]==1&&model.Constants[2]==-1000,"Unit40 minus1000 is written as one without mutating source model");model.Constants[2]=110.5;
                byte[] before=File.ReadAllBytes(path);model.Curves[1].Values[1,99]=1e20;Reject(()=>model.Save(path),"Unit40 last point overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit40 failed save preserves original");model.Curves[1].Values[1,99]=100.75;
                model.Constants[2]=double.NaN;Reject(()=>model.Save(path),"Unit40 nonfinite constant rejected");model.Constants[2]=110.5;model.Curves[1].Count=101;Reject(()=>model.Save(path),"Unit40 point capacity enforced");model.Curves[1].Count=100;
                string source=Path.Combine(sourceRoot,"sp-12","data","ReLPT.dat");before=File.ReadAllBytes(source);var original=InputFile57.Load(source,CultureInfo.InvariantCulture);original.Save(path,CultureInfo.InvariantCulture);Check(Equal(original,InputFile57.Load(path,CultureInfo.InvariantCulture)),"Unit40 original ReLPT round trip");Check(original.Curves.All(c=>c.Count==6&&c.Parameter==574)&&original.Constants.SequenceEqual(new[]{1.582e-5,293d,110d}),"Unit40 original constants and parameter IDs");Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit40 original unchanged");
                var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);lines[6]="101";File.WriteAllLines(path,lines,LegacyCodec.TextEncoding);Reject(()=>InputFile57.Load(path),"Unit40 invalid second count rejected");lines[6]="6";lines[9]="1 2";File.WriteAllLines(path,lines,LegacyCodec.TextEncoding);Reject(()=>InputFile57.Load(path),"Unit40 incomplete second row rejected");File.WriteAllText(path,"57 incomplete\r\n",LegacyCodec.TextEncoding);Reject(()=>InputFile57.Load(path),"Unit40 incomplete file rejected");
            }
            finally{Directory.Delete(temp,true);}
        }
        private static bool Equal(InputFile57 a,InputFile57 b)=>a.Comment==b.Comment&&a.Constants.SequenceEqual(b.Constants)&&a.ConstantsSuffix==b.ConstantsSuffix&&a.Curves.Zip(b.Curves,(x,y)=>x.Count==y.Count&&x.Parameter==y.Parameter&&x.Values.Cast<double>().SequenceEqual(y.Values.Cast<double>())).All(v=>v)&&a.TrailingLines.SequenceEqual(b.TrailingLines);
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
