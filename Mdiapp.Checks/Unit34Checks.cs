using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit34Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit34Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"300.dat");var model=new InputFile300{Comment="Проверка",AdditionalLineCount=4};
                for(int i=0;i<14;i++){model.Curves[i].Count=i==13?100:i+1;model.Curves[i].Parameter=i==13?1999:i;for(int j=0;j<model.Curves[i].Count;j++){model.Curves[i].Values[0,j]=j+.125;model.Curves[i].Values[1,j]=i+j+.75;}}
                for(int i=0;i<2;i++){for(int j=0;j<3;j++)model.InitialValues[i][j]=i+j+.25;for(int j=0;j<8;j++)model.Limits[i][j]=10*i+j+.5;model.Constants[i]=i+.625;}
                for(int i=0;i<4;i++)for(int j=0;j<model.AdditionalValues[i].Length;j++)model.AdditionalValues[i][j]=i+j+.125;
                model.Save(path,CultureInfo.InvariantCulture);var saved=InputFile300.Load(path,CultureInfo.InvariantCulture);
                Check(Equal(model,saved),"Unit34 fourteen curves, interleaved scalars, 100 points and unknown parameter");
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(Equal(model,InputFile300.Load(path,CultureInfo.GetCultureInfo("ru-RU")))&&File.Exists(path+".bak"),"Unit34 comma culture and backup");
                byte[] before=File.ReadAllBytes(path);model.Curves[13].Values[1,99]=1e20;Reject(()=>model.Save(path),"Unit34 last curve overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit34 invalid save preserves file");model.Curves[13].Values[1,99]=99.75;
                var legacy=new InputFile300();legacy.Save(path);Check(InputFile300.Load(path).AdditionalLineCount==0,"Unit34 older file without optional records");legacy.AdditionalValues[3][3]=42;legacy.Save(path);Check(InputFile300.Load(path).AdditionalValues[3][3]==42,"Unit34 editing last optional record writes preceding records");
                File.WriteAllText(path,"300 truncated\r\n",LegacyCodec.TextEncoding);Reject(()=>InputFile300.Load(path),"Unit34 truncated input rejected");
                foreach(string name in new[]{"MEH.dat","MEH1.dat"})
                {
                    string source=Path.Combine(sourceRoot,"sp-12","data",name);before=File.ReadAllBytes(source);var real=InputFile300.Load(source,CultureInfo.InvariantCulture);real.Save(path,CultureInfo.InvariantCulture);Check(Equal(real,InputFile300.Load(path,CultureInfo.InvariantCulture)),"Unit34 original round trip "+name);Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit34 original unchanged "+name);
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static bool Equal(InputFile300 a,InputFile300 b)=>a.Comment==b.Comment&&a.InitialValues.SelectMany(v=>v).SequenceEqual(b.InitialValues.SelectMany(v=>v))&&a.Limits.SelectMany(v=>v).SequenceEqual(b.Limits.SelectMany(v=>v))&&a.Constants.SequenceEqual(b.Constants)&&a.AdditionalValues.SelectMany(v=>v).SequenceEqual(b.AdditionalValues.SelectMany(v=>v))&&a.Curves.Zip(b.Curves,(x,y)=>x.Count==y.Count&&x.Parameter==y.Parameter&&x.Values.Cast<double>().SequenceEqual(y.Values.Cast<double>())).All(v=>v);
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
