using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit27Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit27Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"input.dat");var input=new InputCurveFile{Type=1,Count=2,Parameter=158,CountSuffix="  1  2  2  4"};input.Values[0,0]=3;input.Values[1,0]=2;input.Values[0,1]=4;input.Values[1,1]=1;input.Save(path,CultureInfo.InvariantCulture);
                var model=CorrectedCurveFile.Load(path);model.A=0.5;model.B=0.25;model.C=0.1;Check(Math.Abs(model.Correct(3,2)-3.325)<1e-12,"Unit27 original correction formula");
                string output=Path.Combine(temp,"output.dat");model.Save(output,true,CultureInfo.InvariantCulture);var baked=CorrectedCurveFile.Load(output);
                Check(baked.Curve.Count==2&&baked.Curve.Values[1,0]==3.325&&baked.References.SequenceEqual(new[]{0d,1,0,1}),"Unit27 corrected save retains count and neutral references");
                model.Save(output,true,CultureInfo.InvariantCulture);Check(CorrectedCurveFile.Load(output).Curve.Values[1,0]==3.325&&model.Curve.Values[1,0]==2,"Unit27 repeated save does not compound correction");
                model.Save(output,false,CultureInfo.GetCultureInfo("ru-RU"));var unbaked=CorrectedCurveFile.Load(output,CultureInfo.GetCultureInfo("ru-RU"));Check(unbaked.Curve.Values[1,0]==2&&unbaked.References.SequenceEqual(model.References)&&File.Exists(output+".bak"),"Unit27 uncorrected comma save and backup");
                model.A=1;model.B=0;model.C=0;model.References[0]=1;model.References[1]=0;Check(model.Correct(2,4)==8,"Unit27 zero denominator uses one");model.A=-1000;Check(model.Correct(2,4)==4,"Unit27 unavailable coefficient sentinel");
                byte[] before=File.ReadAllBytes(output);model.A=123456789;Reject(()=>model.Save(output,true),"Unit27 corrected overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(output)),"Unit27 invalid correction preserves file");
                input.Type=52;input.Save(path,CultureInfo.InvariantCulture);Reject(()=>CorrectedCurveFile.Load(path),"Unit27 wrong type rejected");
                if(sourceRoot!=null)foreach(string name in new[]{"XVHOD.dat","XVHOD1.dat","XVHOD2.dat"})
                {
                    string source=Path.Combine(sourceRoot,"sp-12","data",name);before=File.ReadAllBytes(source);var real=CorrectedCurveFile.Load(source,CultureInfo.InvariantCulture);real.Save(output,false,CultureInfo.InvariantCulture);var copy=CorrectedCurveFile.Load(output,CultureInfo.InvariantCulture);
                    Check(real.Curve.Count==copy.Curve.Count&&real.Curve.Parameter==copy.Curve.Parameter&&real.References.SequenceEqual(copy.References)&&real.Curve.Values.Cast<double>().SequenceEqual(copy.Curve.Values.Cast<double>()),"Unit27 original round trip "+name);
                    Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit27 original unchanged "+name);
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
