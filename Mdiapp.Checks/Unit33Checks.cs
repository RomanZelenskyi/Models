using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit33Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit33Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"triple.dat");var input=new InputTripleCurveFile{Type=21};input.Curves[0].CountSuffix="  1  2";input.Curves[1].CountSuffix="  3  4  5  6";input.Curves[2].CountSuffix="  7  8";
                for(int curve=0;curve<3;curve++){input.Curves[curve].Count=100;for(int point=0;point<100;point++){input.Curves[curve].Values[0,point]=9;input.Curves[curve].Values[1,point]=2;}}input.Save(path,CultureInfo.InvariantCulture);
                var model=CorrectedTripleCurveFile.Load(path);for(int i=0;i<3;i++)model.Coefficients[i,0]=1;model.Coefficients[2,1]=0.5;
                Check(model.References[1].SequenceEqual(new[]{7d,8})&&model.References[2].SequenceEqual(new[]{3d,4,5,6}),"Unit33 original cross-block reference mapping");
                Check(model.Correct(0,9,2)==10&&model.Correct(1,9,2)==2.5&&Math.Abs(model.Correct(2,9,2)-17d/3)<1e-12,"Unit33 three independent formulas");
                model.Save(path,true,CultureInfo.InvariantCulture);var saved=CorrectedTripleCurveFile.Load(path);
                Check(saved.Data.Curves.All(c=>c.Count==100)&&saved.Data.Curves[0].Values[1,0]==10&&saved.Data.Curves[1].Values[1,0]==2.5&&saved.References[2].SequenceEqual(new[]{0d,1,0,1}),"Unit33 corrected save retains all counts and neutral references");
                model.Save(path,true,CultureInfo.InvariantCulture);Check(CorrectedTripleCurveFile.Load(path).Data.Curves[0].Values[1,0]==10&&model.Data.Curves[0].Values[1,0]==2,"Unit33 repeated save does not compound");
                model.Save(path,false,CultureInfo.GetCultureInfo("ru-RU"));Check(CorrectedTripleCurveFile.Load(path,CultureInfo.GetCultureInfo("ru-RU")).References[1].SequenceEqual(new[]{7d,8})&&File.Exists(path+".bak"),"Unit33 comma uncorrected save and backup");
                byte[] before=File.ReadAllBytes(path);model.Coefficients[2,2]=123456789;Reject(()=>model.Save(path,true),"Unit33 third curve overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit33 invalid correction preserves file");
                if(sourceRoot!=null)foreach(string name in new[]{"XSOPL1.dat","XSOPL2.dat"})
                {
                    string source=Path.Combine(sourceRoot,"sp-12","data",name);before=File.ReadAllBytes(source);var real=CorrectedTripleCurveFile.Load(source,CultureInfo.InvariantCulture);real.Save(path,false,CultureInfo.InvariantCulture);var copy=CorrectedTripleCurveFile.Load(path,CultureInfo.InvariantCulture);
                    Check(real.Data.Type==copy.Data.Type&&real.Data.Curves.Zip(copy.Data.Curves,(a,b)=>a.Count==b.Count&&a.Parameter==b.Parameter&&a.Values.Cast<double>().SequenceEqual(b.Values.Cast<double>())).All(v=>v)&&real.References.Zip(copy.References,(a,b)=>a.SequenceEqual(b)).All(v=>v),"Unit33 original round trip "+name);Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit33 original unchanged "+name);
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
