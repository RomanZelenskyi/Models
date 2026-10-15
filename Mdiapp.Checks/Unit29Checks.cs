using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit29Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit29Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"curve.dat");
                foreach(int type in new[]{11,16,20})
                {
                    var input=new InputCurveFile{Type=type,Count=100,Parameter=545,CountSuffix="  1  2"};for(int i=0;i<100;i++){input.Values[0,i]=3+i/100d;input.Values[1,i]=2;}input.Save(path,CultureInfo.InvariantCulture);
                    var model=CorrectedCurveFile.Load(path,simple:true);model.A=0.5;model.C=0.1;Check(Math.Abs(model.Correct(3,2)-3.2)<1e-12,"Unit29 formula type "+type);
                    model.Save(path,true,CultureInfo.InvariantCulture);var saved=CorrectedCurveFile.Load(path,simple:true);
                    Check(saved.Curve.Type==type&&saved.Curve.Count==100&&saved.Curve.Values[1,0]==3.2&&saved.References.Take(2).SequenceEqual(new[]{0d,1}),"Unit29 corrected count and neutral references type "+type);
                    model.Save(path,true,CultureInfo.InvariantCulture);Check(CorrectedCurveFile.Load(path,simple:true).Curve.Values[1,0]==3.2&&model.Curve.Values[1,0]==2,"Unit29 repeated save does not compound type "+type);
                    model.Save(path,false,CultureInfo.GetCultureInfo("ru-RU"));Check(CorrectedCurveFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"),true).References.Take(2).SequenceEqual(new[]{1d,2})&&File.Exists(path+".bak"),"Unit29 comma uncorrected save type "+type);
                }
                byte[] before=File.ReadAllBytes(path);var invalid=CorrectedCurveFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"),true);invalid.C=123456789;Reject(()=>invalid.Save(path,true),"Unit29 corrected overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit29 invalid correction preserves original");
                if(sourceRoot!=null)foreach(string name in new[]{"XPER2.dat","XPER3.dat","XPER5.dat"})
                {
                    string source=Path.Combine(sourceRoot,"sp-12","data",name);before=File.ReadAllBytes(source);var real=CorrectedCurveFile.Load(source,CultureInfo.InvariantCulture,true);real.Save(path,false,CultureInfo.InvariantCulture);var saved=CorrectedCurveFile.Load(path,CultureInfo.InvariantCulture,true);
                    Check(real.Curve.Type==saved.Curve.Type&&real.Curve.Count==saved.Curve.Count&&real.Curve.Parameter==saved.Curve.Parameter&&real.Curve.Values.Cast<double>().SequenceEqual(saved.Curve.Values.Cast<double>()),"Unit29 original round trip "+name);Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit29 original unchanged "+name);
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
