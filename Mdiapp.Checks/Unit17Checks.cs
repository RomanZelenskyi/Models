using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit17Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit17Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                var model=new InputCurveFile{Type=52,Count=100,Parameter=543,Comment="Тест",XCaption="X",YCaption="Y"};
                for(int i=0;i<100;i++){model.Values[0,i]=(i-50)/10d;model.Values[1,i]=i/1000d;}
                string path=Path.Combine(temp,"curve.dat");model.Save(path,CultureInfo.InvariantCulture);
                var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);
                Check(lines.Length==5&&lines[3].Length==1407&&lines[3].Substring(0,14)=="     -5.000000","Unit17 100 points F6 width 14");
                var copy=InputCurveFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"));
                Check(copy.Values.Cast<double>().SequenceEqual(model.Values.Cast<double>())&&copy.Parameter==543&&copy.Count==100,"Unit17 dot values and ID round trip");
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(InputCurveFile.Load(path,CultureInfo.GetCultureInfo("ru-RU")).Values.Cast<double>().SequenceEqual(model.Values.Cast<double>())&&File.Exists(path+".bak"),"Unit17 comma values and backup");
                byte[] before=File.ReadAllBytes(path);model.Values[1,99]=123456789;
                Reject(()=>model.Save(path),"Unit17 overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit17 failed validation preserves file");
                model.Values[1,99]=double.NaN;Reject(()=>model.Save(path),"Unit17 nonfinite value rejected");model.Count=101;Reject(()=>model.Save(path),"Unit17 excessive count rejected");
                var paste=InputCurveFile.ParseClipboardRows("1,25\t2.5\r\n3\tbad\r\n4\t5",CultureInfo.GetCultureInfo("ru-RU"));Check(paste.Length==2&&paste[0][0]==1.25&&paste[1][1]==0,"Unit17 two clipboard rows and cultures");
                File.WriteAllText(path,"52 Short\r\n",LegacyCodec.TextEncoding);Reject(()=>InputCurveFile.Load(path),"Unit17 incomplete file rejected");
                if(sourceRoot!=null)foreach(string name in new[]{"clearance.dat","XVHOD.dat","XVHOD1.dat","XVHOD2.dat"})
                {
                    string source=Path.Combine(sourceRoot,"sp-12","data",name);byte[] original=File.ReadAllBytes(source);var real=InputCurveFile.Load(source,CultureInfo.InvariantCulture);real.Save(path,CultureInfo.InvariantCulture);var saved=InputCurveFile.Load(path,CultureInfo.InvariantCulture);
                    Check(saved.Type==real.Type&&saved.Parameter==real.Parameter&&saved.Count==real.Count&&saved.CountSuffix==real.CountSuffix&&saved.Values.Cast<double>().SequenceEqual(real.Values.Cast<double>()),"Unit17 real round trip "+name);
                    Check(original.SequenceEqual(File.ReadAllBytes(source)),"Unit17 source unchanged "+name);
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
