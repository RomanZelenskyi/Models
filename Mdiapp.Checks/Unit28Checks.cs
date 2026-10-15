using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit28Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit28Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"14.dat");var data=new InputDoubleCurveFile();data.Curves[0].CountSuffix="  1  2  2  4";data.Curves[0].Count=2;data.Curves[1].Count=3;
                for(int curve=0;curve<2;curve++)for(int i=0;i<data.Curves[curve].Count;i++){data.Curves[curve].Values[0,i]=3+i;data.Curves[curve].Values[1,i]=2;}
                data.Constants[0]=230000.33;data.Constants[1]=43150000;data.Constants[2]=14.9;data.Save(path,CultureInfo.InvariantCulture);
                var model=CorrectedDoubleCurveFile.Load(path);model.Coefficients[0,0]=0.5;model.Coefficients[0,1]=0.25;model.Coefficients[0,2]=0.1;model.Coefficients[1,2]=0.25;
                Check(Math.Abs(model.Correct(0,3,2)-3.325)<1e-12&&model.Correct(1,3,2)==2.5,"Unit28 two independent correction formulas");
                model.Save(path,true,CultureInfo.InvariantCulture);var saved=CorrectedDoubleCurveFile.Load(path);
                Check(saved.Data.Curves[0].Count==2&&saved.Data.Curves[1].Count==3&&saved.Data.Curves[0].Values[1,0]==3.325&&saved.Data.Curves[1].Values[1,0]==2.5&&saved.References.SequenceEqual(new[]{0d,1,0,1}),"Unit28 corrected save preserves counts and neutral references");
                Check(saved.Data.Constants.SequenceEqual(data.Constants),"Unit28 correction does not change constants");
                model.Save(path,true,CultureInfo.InvariantCulture);Check(CorrectedDoubleCurveFile.Load(path).Data.Curves[0].Values[1,0]==3.325&&model.Data.Curves[0].Values[1,0]==2,"Unit28 repeated save does not compound correction");
                model.Save(path,false,CultureInfo.GetCultureInfo("ru-RU"));Check(CorrectedDoubleCurveFile.Load(path,CultureInfo.GetCultureInfo("ru-RU")).References.SequenceEqual(model.References)&&File.Exists(path+".bak"),"Unit28 uncorrected comma save and backup");
                byte[] before=File.ReadAllBytes(path);model.Coefficients[1,2]=123456789;Reject(()=>model.Save(path,true),"Unit28 second curve overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit28 second correction failure preserves file");
                if(sourceRoot!=null)
                {
                    string source=Path.Combine(sourceRoot,"sp-12","data","XKS.dat");before=File.ReadAllBytes(source);var real=CorrectedDoubleCurveFile.Load(source,CultureInfo.InvariantCulture);real.Save(path,false,CultureInfo.InvariantCulture);var copy=CorrectedDoubleCurveFile.Load(path,CultureInfo.InvariantCulture);
                    Check(real.Data.Curves.Zip(copy.Data.Curves,(a,b)=>a.Count==b.Count&&a.Parameter==b.Parameter&&a.Values.Cast<double>().SequenceEqual(b.Values.Cast<double>())).All(v=>v)&&real.References.SequenceEqual(copy.References)&&real.Data.Constants.SequenceEqual(copy.Data.Constants),"Unit28 original XKS round trip");
                    Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit28 original unchanged");
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
