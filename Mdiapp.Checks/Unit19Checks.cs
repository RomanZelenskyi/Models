using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit19Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit19Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                var model=new InputDoubleCurveFile{Comment="Тест",ConstantsCaption="Константы"};
                model.Curves[0].Count=100;model.Curves[1].Count=3;model.Curves[0].Parameter=546;model.Curves[1].Parameter=1999;
                for(int group=0;group<2;group++)for(int row=0;row<2;row++)for(int col=0;col<model.Curves[group].Count;col++)model.Curves[group].Values[row,col]=(col-row)/100d;
                model.Constants[0]=230000.33;model.Constants[1]=43150000;model.Constants[2]=14.9;
                string path=Path.Combine(temp,"14.dat");model.Save(path,CultureInfo.InvariantCulture);var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);
                Check(lines.Length==10&&lines[3].Length==1406&&lines[9].StartsWith("    230000.330  43150000.000        14.900"),"Unit19 two curve blocks F6 and constants F3");
                var saved=InputDoubleCurveFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"));
                Check(saved.Curves[0].Count==100&&saved.Curves[1].Count==3&&saved.Curves[1].Parameter==1999&&saved.Constants.SequenceEqual(model.Constants),"Unit19 independent counts IDs and constants");
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(InputDoubleCurveFile.Load(path,CultureInfo.GetCultureInfo("ru-RU")).Curves[0].Values.Cast<double>().SequenceEqual(model.Curves[0].Values.Cast<double>())&&File.Exists(path+".bak"),"Unit19 decimal comma and backup");
                var before=File.ReadAllBytes(path);model.Curves[1].Values[1,2]=123456789;
                Reject(()=>model.Save(path),"Unit19 second curve overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit19 second curve validation preserves whole file");
                model.Curves[1].Values[1,2]=0;model.Constants[0]=double.PositiveInfinity;Reject(()=>model.Save(path),"Unit19 nonfinite constant rejected");
                File.WriteAllText(path,"14 Short\r\n",LegacyCodec.TextEncoding);Reject(()=>InputDoubleCurveFile.Load(path),"Unit19 incomplete file rejected");
                if(sourceRoot!=null)
                {
                    string source=Path.Combine(sourceRoot,"sp-12","data","XKS.dat");before=File.ReadAllBytes(source);var real=InputDoubleCurveFile.Load(source,CultureInfo.InvariantCulture);
                    Check(real.Curves[0].Count==5&&real.Curves[1].Count==6&&real.Curves[0].Parameter==546&&real.Curves[1].Parameter==547&&real.Constants[1]==43150000,"Unit19 original XKS counts parameters and constants");
                    real.Save(path,CultureInfo.InvariantCulture);var loaded=InputDoubleCurveFile.Load(path,CultureInfo.InvariantCulture);
                    Check(real.Constants.SequenceEqual(loaded.Constants)&&real.Curves.Zip(loaded.Curves,(a,b)=>a.Count==b.Count&&a.Parameter==b.Parameter&&a.CountSuffix==b.CountSuffix&&a.Values.Cast<double>().SequenceEqual(b.Values.Cast<double>())).All(v=>v),"Unit19 original XKS round trip");
                    Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit19 original unchanged");
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
