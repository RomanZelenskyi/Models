using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit31Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit31Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"turbine.dat");var data=new CharacteristicFile{Type=15,Interpolation=5,SpecialSuffix="  1  2  1  4"};for(int i=0;i<4;i++)data.Coefficients[i]=i+2;
                data.Branches.Add(new CharacteristicBranch{Value=2,Points=new double[,]{{10,20},{3,5},{1,2}}});data.Save(path,CultureInfo.InvariantCulture,true);
                var model=CorrectedCharacteristicFile.Load(path,turbine:true);model.Coefficients[0,0]=0.5;model.Coefficients[0,1]=0.25;model.Coefficients[0,2]=0.1;model.Coefficients[1,2]=0.25;
                var points=model.Correct(model.Data.Branches[0]);Check(Math.Abs(points[0,0]-16.625)<1e-12&&points[1,0]==3&&points[2,0]==1.25,"Unit31 corrects first and third rows preserving second");
                model.References[3]=8;Check(Math.Abs(model.Correct(model.Data.Branches[0])[0,0]-16.3125)<1e-12,"Unit31 fourth reference is independent of third");
                model.Save(path,true,CultureInfo.InvariantCulture);var saved=CorrectedCharacteristicFile.Load(path,turbine:true);
                Check(saved.Data.Interpolation==5&&saved.Data.Coefficients.SequenceEqual(data.Coefficients)&&saved.References.SequenceEqual(new[]{0d,1,0,1}),"Unit31 corrected save retains interpolation scales and four neutral references");
                model.Save(path,true,CultureInfo.InvariantCulture);Check(CorrectedCharacteristicFile.Load(path,turbine:true).Data.Branches[0].Points[0,0]==16.313&&model.Data.Branches[0].Points[0,0]==10,"Unit31 repeated save does not compound");
                model.Save(path,false,CultureInfo.GetCultureInfo("ru-RU"));Check(CorrectedCharacteristicFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"),true).References.SequenceEqual(model.References)&&File.Exists(path+".bak"),"Unit31 comma save and backup");
                byte[] before=File.ReadAllBytes(path);model.Coefficients[1,2]=123456789;Reject(()=>model.Save(path,true),"Unit31 third row overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit31 invalid correction preserves file");
                if(sourceRoot!=null)foreach(string name in new[]{"Xtvd.dat","Xtnd1.dat","Xtnd.dat"})
                {
                    string source=Path.Combine(sourceRoot,"sp-12","data",name);before=File.ReadAllBytes(source);var real=CorrectedCharacteristicFile.Load(source,CultureInfo.InvariantCulture,true);real.Save(path,false,CultureInfo.InvariantCulture);var copy=CorrectedCharacteristicFile.Load(path,CultureInfo.InvariantCulture,true);
                    Check(real.Data.Type==copy.Data.Type&&real.Data.Interpolation==copy.Data.Interpolation&&real.References.SequenceEqual(copy.References)&&real.Data.Branches.Zip(copy.Data.Branches,(a,b)=>a.Value==b.Value&&a.Points.Cast<double>().SequenceEqual(b.Points.Cast<double>())).All(v=>v),"Unit31 original round trip "+name);Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit31 original unchanged "+name);
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
