using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit36Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit36Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"transition.dat");var model=new InputTransitionFile{Comment="Проверка",Scale=1808.23,ScaleSuffix="   1  40     ! extra values"};model.Curve.Count=100;model.Correction.Resize(30,100);
                for(int point=0;point<100;point++){model.Curve.Values[0,point]=point+.125;model.Curve.Values[1,point]=point+.25;model.Correction.Arguments[point]=point+.5;for(int row=0;row<60;row++)model.Correction.Values[row,point]=row+point+.625;}
                model.Curve.Values[0,0]=1234567.123456;model.Curve.Values[0,1]=-123456.123456;model.TrailingLines.Add("! original results");
                foreach(int type in new[]{43,46,49}){model.Type=type;model.Save(path,CultureInfo.InvariantCulture);Check(Equal(model,InputTransitionFile.Load(path,CultureInfo.InvariantCulture)),"Unit36 type "+type+" 100 points and 30 coefficient branches round trip");}
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(Equal(model,InputTransitionFile.Load(path,CultureInfo.GetCultureInfo("ru-RU")))&&File.Exists(path+".bak"),"Unit36 comma format and fixed fields without separators");
                byte[] before=File.ReadAllBytes(path);model.Correction.Values[59,99]=1e20;Reject(()=>model.Save(path),"Unit36 last coefficient overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit36 failed save preserves original");model.Correction.Values[59,99]=158.625;
                model.Scale=double.NaN;Reject(()=>model.Save(path),"Unit36 nonfinite scale rejected");model.Scale=1808.23;model.ScaleSuffix="\r\n";Reject(()=>model.Save(path),"Unit36 multiline suffix rejected");model.ScaleSuffix="";
                Reject(()=>model.Correction.Resize(31,1),"Unit36 branch capacity enforced");Reject(()=>model.Correction.Resize(1,101),"Unit36 transition point capacity enforced");
                File.WriteAllText(path,"43 incomplete\r\n1\r\n101\r\n",LegacyCodec.TextEncoding);Reject(()=>InputTransitionFile.Load(path),"Unit36 invalid primary count rejected");File.WriteAllText(path,"43 incomplete\r\n",LegacyCodec.TextEncoding);Reject(()=>InputTransitionFile.Load(path),"Unit36 incomplete file rejected");
                string source=Path.Combine(sourceRoot,"sp-12","data","ResultTVDBL.dat");before=File.ReadAllBytes(source);var original=InputTransitionFile.Load(source,CultureInfo.InvariantCulture);original.Save(path,CultureInfo.InvariantCulture);var saved=InputTransitionFile.Load(path,CultureInfo.InvariantCulture);
                Check(original.Type==43&&original.Curve.Count==9&&original.Correction.BranchCount==1&&original.Correction.Arguments.Length==3,"Unit36 original ResultTVDBL dimensions");
                Check(original.Scale==saved.Scale&&original.ScaleSuffix==saved.ScaleSuffix&&original.TrailingLines.SequenceEqual(saved.TrailingLines)&&File.ReadAllLines(path,LegacyCodec.TextEncoding)[5].Contains("("),"Unit36 extra scale values comments and result tail preserved");
                Check(original.Curve.Values.Cast<double>().Zip(saved.Curve.Values.Cast<double>(),(a,b)=>Math.Abs(a-b)<=.00000051).All(v=>v)&&original.Correction.Values.Cast<double>().SequenceEqual(saved.Correction.Values.Cast<double>()),"Unit36 original curves retained within F6 precision");
                Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit36 original unchanged");Reject(()=>InputTransitionFile.Load(Path.Combine(sourceRoot,"sp-12","data","ResultTNDBL.dat")),"Unit36 old Unit23 format rejected");
            }
            finally{Directory.Delete(temp,true);}
        }
        private static bool Equal(InputTransitionFile a,InputTransitionFile b)=>a.Type==b.Type&&a.Comment==b.Comment&&a.Scale==b.Scale&&a.ScaleSuffix==b.ScaleSuffix&&a.Curve.Count==b.Curve.Count&&a.Curve.Values.Cast<double>().SequenceEqual(b.Curve.Values.Cast<double>())&&a.Correction.Arguments.SequenceEqual(b.Correction.Arguments)&&a.Correction.Values.Cast<double>().SequenceEqual(b.Correction.Values.Cast<double>())&&a.TrailingLines.SequenceEqual(b.TrailingLines);
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
