using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit30Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit30Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"branch.dat");var data=new CharacteristicFile{Type=3,Interpolation=10,ControlSuffix="  1  2  0.5"};for(int i=0;i<4;i++)data.Coefficients[i]=1;
                var branch=new CharacteristicBranch{Value=2,Points=new double[,]{{0,10,20},{10,30,20},{1,2,3}}};data.Branches.Add(branch);data.Save(path,CultureInfo.InvariantCulture,true);
                var model=CorrectedCharacteristicFile.Load(path);model.Coefficients[0,0]=0.2;model.Coefficients[0,1]=0.4;model.Coefficients[0,2]=0.1;model.Coefficients[1,2]=0.25;model.XCorrection=0.1;
                var points=model.Correct(model.Data.Branches[0]);Check(points[0,1]==11&&Math.Abs(points[1,1]-36)<1e-12&&points[2,2]==3.75,"Unit30 normalized first row and independent corrections");
                model.Data.Interpolation=5;points=model.Correct(model.Data.Branches[0]);Check(Math.Abs(points[1,1]-42)<1e-12,"Unit30 normalization follows interpolation row");
                model.Save(path,true,CultureInfo.InvariantCulture);var copy=CorrectedCharacteristicFile.Load(path);Check(copy.Data.Branches[0].Points[0,1]==11&&copy.Data.Coefficients.All(v=>v==1)&&copy.Data.Coefficients.Length==4,"Unit30 corrected save retains four scale coefficients");
                model.Save(path,true,CultureInfo.InvariantCulture);Check(CorrectedCharacteristicFile.Load(path).Data.Branches[0].Points[0,1]==11&&model.Data.Branches[0].Points[0,1]==10,"Unit30 repeated save does not compound correction");
                model.Save(path,false,CultureInfo.GetCultureInfo("ru-RU"));Check(CorrectedCharacteristicFile.Load(path,CultureInfo.GetCultureInfo("ru-RU")).References.SequenceEqual(model.References)&&File.Exists(path+".bak"),"Unit30 uncorrected comma save and backup");
                byte[] before=File.ReadAllBytes(path);model.XCorrection=123456789;Reject(()=>model.Save(path,true),"Unit30 corrected field overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit30 invalid correction preserves original");
                data.Type=1240;data.Branches[0].Points=new double[3,100];data.Save(path,CultureInfo.InvariantCulture,true);Check(CorrectedCharacteristicFile.Load(path).Data.Branches[0].Points.GetLength(1)==100,"Unit30 variable branch header supports 100 points");
                if(sourceRoot!=null)foreach(string name in new[]{"Xknd1.dat","Xknd2.dat"})
                {
                    string source=Path.Combine(sourceRoot,"sp-12","data",name);before=File.ReadAllBytes(source);var real=CorrectedCharacteristicFile.Load(source,CultureInfo.InvariantCulture);real.Save(path,false,CultureInfo.InvariantCulture);var saved=CorrectedCharacteristicFile.Load(path,CultureInfo.InvariantCulture);
                    Check(real.Data.Type==saved.Data.Type&&real.Data.Branches.Zip(saved.Data.Branches,(a,b)=>a.Value==b.Value&&a.Points.Cast<double>().SequenceEqual(b.Points.Cast<double>())).All(v=>v)&&real.References.SequenceEqual(saved.References),"Unit30 original round trip "+name);Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit30 original unchanged "+name);
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
