using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit20Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit20Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                var model=new InputTripleCurveFile{Type=27,Comment="Три характеристики"};int[] counts={2,11,100};
                for(int group=0;group<3;group++)
                {
                    var curve=model.Curves[group];curve.Count=counts[group];curve.Parameter=group==2?1999:260+group;
                    for(int row=0;row<2;row++)for(int col=0;col<curve.Count;col++)curve.Values[row,col]=(group*100+col-row)/100d;
                }
                string path=Path.Combine(temp,"triple.dat");model.Save(path,CultureInfo.InvariantCulture);var saved=InputTripleCurveFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"));
                Check(saved.Type==27&&saved.Curves.Select(c=>c.Count).SequenceEqual(counts)&&saved.Curves[2].Parameter==1999,"Unit20 type 27 independent counts and IDs");
                Check(File.ReadAllLines(path).Length==13&&model.Curves.Zip(saved.Curves,(a,b)=>a.Values.Cast<double>().SequenceEqual(b.Values.Cast<double>())).All(v=>v),"Unit20 three blocks and 100 points round trip");
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(InputTripleCurveFile.Load(path,CultureInfo.GetCultureInfo("ru-RU")).Curves[2].Values[1,99]==2.98&&File.Exists(path+".bak"),"Unit20 comma round trip and backup");
                byte[] before=File.ReadAllBytes(path);model.Curves[2].Values[1,99]=123456789;Reject(()=>model.Save(path),"Unit20 third curve overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit20 third curve failure preserves whole file");
                model.Curves[2].Values[1,99]=0;model.Curves[2].Count=101;Reject(()=>model.Save(path),"Unit20 excessive count rejected");
                File.WriteAllText(path,"27 Short\r\n",LegacyCodec.TextEncoding);Reject(()=>InputTripleCurveFile.Load(path),"Unit20 incomplete third curve rejected");
                if(sourceRoot!=null)foreach(string name in new[]{"XSOPL1.dat","XSOPL2.dat"})
                {
                    string source=Path.Combine(sourceRoot,"sp-12","data",name);before=File.ReadAllBytes(source);var real=InputTripleCurveFile.Load(source,CultureInfo.InvariantCulture);real.Save(path,CultureInfo.InvariantCulture);var copy=InputTripleCurveFile.Load(path,CultureInfo.InvariantCulture);
                    Check(real.Type==copy.Type&&real.Curves.Zip(copy.Curves,(a,b)=>a.Count==b.Count&&a.Parameter==b.Parameter&&a.CountSuffix==b.CountSuffix&&a.Values.Cast<double>().SequenceEqual(b.Values.Cast<double>())).All(v=>v),"Unit20 original round trip "+name);
                    Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit20 original unchanged "+name);
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
