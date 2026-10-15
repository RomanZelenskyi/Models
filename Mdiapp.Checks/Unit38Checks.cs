using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit38Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit38Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"55.dat");var model=new InputFile55{Comment="Проверка"};
                for(int i=0;i<15;i++){var curve=model.Curves[i];curve.Parameter=i==14?1999:i;curve.Count=i==14?100:i+1;curve.ParameterCaption="Блок "+InputFile55.GridOrder[i];curve.XCaption="X "+i;curve.YCaption="Y "+i;for(int j=0;j<curve.Count;j++){curve.Values[0,j]=j+.125;curve.Values[1,j]=i+j+.75;}}
                model.Curves[0].Values[1,0]=1.25e-12;model.Curves[1].Values[1,0]=1234567890120000;model.TrailingLines.Add("! remaining data");model.Save(path,CultureInfo.InvariantCulture);
                Check(Equal(model,InputFile55.Load(path,CultureInfo.InvariantCulture)),"Unit38 fifteen curves 100 points unknown parameter and general numeric format");Check(InputFile55.GridOrder.SequenceEqual(new[]{1,2,3,6,7,8,10,11,12,13,16,17,18,15,19}),"Unit38 original physical block order");
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(Equal(model,InputFile55.Load(path,CultureInfo.GetCultureInfo("ru-RU")))&&File.Exists(path+".bak"),"Unit38 comma culture and backup");
                byte[] before=File.ReadAllBytes(path);model.Curves[14].Values[1,99]=double.PositiveInfinity;Reject(()=>model.Save(path),"Unit38 last curve nonfinite value rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit38 invalid last block preserves original");model.Curves[14].Values[1,99]=113.75;
                model.Curves[14].Count=101;Reject(()=>model.Save(path),"Unit38 point capacity enforced");model.Curves[14].Count=100;model.Curves[14].Parameter=2001;Reject(()=>model.Save(path),"Unit38 parameter range enforced");model.Curves[14].Parameter=1999;
                File.WriteAllText(path,"55 incomplete\r\n",LegacyCodec.TextEncoding);Reject(()=>InputFile55.Load(path),"Unit38 incomplete file rejected");
                string source=Path.Combine(sourceRoot,"sp-12","data","otbor_dop.dat");before=File.ReadAllBytes(source);var real=InputFile55.Load(source,CultureInfo.InvariantCulture);real.Save(path,CultureInfo.InvariantCulture);Check(Equal(real,InputFile55.Load(path,CultureInfo.InvariantCulture)),"Unit38 original otbor_dop round trip");Check(real.Curves[13].Count==3&&real.Curves[14].Count==4,"Unit38 independent final block counts");Check(File.ReadAllLines(path,LegacyCodec.TextEncoding)[1]==File.ReadAllLines(source,LegacyCodec.TextEncoding)[1],"Unit38 original parameter comment preserved");Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit38 original unchanged");
                var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);lines[58]="101";File.WriteAllLines(path,lines,LegacyCodec.TextEncoding);Reject(()=>InputFile55.Load(path),"Unit38 invalid last input count rejected");lines[58]="4";lines[60]="0 0";File.WriteAllLines(path,lines,LegacyCodec.TextEncoding);Reject(()=>InputFile55.Load(path),"Unit38 short last input row rejected");
            }
            finally{Directory.Delete(temp,true);}
        }
        private static bool Equal(InputFile55 a,InputFile55 b)=>a.Comment==b.Comment&&a.Curves.Zip(b.Curves,(x,y)=>x.Count==y.Count&&x.Parameter==y.Parameter&&x.Values.Cast<double>().SequenceEqual(y.Values.Cast<double>())&&x.ParameterCaption==y.ParameterCaption&&x.XCaption==y.XCaption&&x.YCaption==y.YCaption).All(v=>v)&&a.TrailingLines.SequenceEqual(b.TrailingLines);
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
