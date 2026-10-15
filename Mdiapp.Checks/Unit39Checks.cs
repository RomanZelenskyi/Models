using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit39Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit39Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"56.dat");var model=new InputFile56{Comment="Проверка"};for(int group=0;group<2;group++)for(int i=0;i<4;i++){model.AdditionalValues[group,i]=group+i+.125;for(int mode=0;mode<2;mode++)model.Coefficients[group,mode,i]=group*100+mode*10+i+.625;}model.TrailingLines.Add("! extra data");
                for(int a=1;a<=2;a++)for(int b=1;b<=2;b++){model.Modes[0]=a;model.Modes[1]=b;model.Save(path,CultureInfo.InvariantCulture);Check(Equal(model,InputFile56.Load(path,CultureInfo.InvariantCulture)),"Unit39 independent modes "+a+"/"+b);}
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(Equal(model,InputFile56.Load(path,CultureInfo.GetCultureInfo("ru-RU")))&&File.Exists(path+".bak"),"Unit39 comma format and backup");
                byte[] before=File.ReadAllBytes(path);model.AdditionalValues[1,3]=double.PositiveInfinity;Reject(()=>model.Save(path),"Unit39 invalid last value rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit39 failed save keeps source intact");model.AdditionalValues[1,3]=4.125;
                model.Modes[1]=3;Reject(()=>model.Save(path),"Unit39 unsupported mode rejected");model.Modes[1]=2;model.Coefficients[1,1,3]=double.NaN;Reject(()=>model.Save(path),"Unit39 nonfinite active coefficient rejected");
                string source=Path.Combine(sourceRoot,"sp-12","data","ParTP.dat");before=File.ReadAllBytes(source);var original=InputFile56.Load(source,CultureInfo.InvariantCulture);original.Save(path,CultureInfo.InvariantCulture);Check(Equal(original,InputFile56.Load(path,CultureInfo.InvariantCulture)),"Unit39 original ParTP round trip");Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit39 original unchanged");
                var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);lines[1]+="   ! mode comment";lines[3]+="   ! coefficient comment";File.WriteAllLines(path,lines,LegacyCodec.TextEncoding);var commented=InputFile56.Load(path);commented.Save(path);Check(File.ReadAllLines(path,LegacyCodec.TextEncoding)[1].Contains("! mode comment")&&File.ReadAllLines(path,LegacyCodec.TextEncoding)[3].Contains("! coefficient comment"),"Unit39 row comments retained");
                lines[4]="0";File.WriteAllLines(path,lines,LegacyCodec.TextEncoding);Reject(()=>InputFile56.Load(path),"Unit39 invalid input mode rejected");lines[4]="1";lines[6]="1 2 3";File.WriteAllLines(path,lines,LegacyCodec.TextEncoding);Reject(()=>InputFile56.Load(path),"Unit39 short coefficient row rejected");File.WriteAllText(path,"56 incomplete\r\n",LegacyCodec.TextEncoding);Reject(()=>InputFile56.Load(path),"Unit39 incomplete file rejected");
            }
            finally{Directory.Delete(temp,true);}
        }
        private static bool Equal(InputFile56 a,InputFile56 b)=>a.Comment==b.Comment&&a.Modes.SequenceEqual(b.Modes)&&a.AdditionalValues.Cast<double>().SequenceEqual(b.AdditionalValues.Cast<double>())&&Enumerable.Range(0,2).All(g=>Enumerable.Range(0,4).All(i=>a.Coefficients[g,a.Modes[g]-1,i]==b.Coefficients[g,b.Modes[g]-1,i]))&&a.TrailingLines.SequenceEqual(b.TrailingLines);
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
