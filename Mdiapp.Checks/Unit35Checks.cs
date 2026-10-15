using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit35Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit35Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"surface.dat");var model=new InputSurfaceFile{Comment="Проверка"};model.Resize(100,3,2,2);
                for(int x=0;x<100;x++)model.X[x]=x+.125;for(int y=0;y<3;y++)model.Y[y]=y+.25;for(int t=0;t<2;t++){model.Temperatures[t]=400+t*100;for(int b=0;b<2;b++){model.BranchValues[t,b]=b+.5;for(int y=0;y<3;y++)for(int x=0;x<100;x++)model.Values[t,b,y,x]=t*1000+b*100+y+x+.625;}}
                for(int i=0;i<2;i++){model.Corrections[i].Resize(30,100);for(int x=0;x<100;x++){model.Corrections[i].Arguments[x]=x+.125;for(int row=0;row<60;row++)model.Corrections[i].Values[row,x]=i+row+x+.75;}}
                model.TrailingLines.Add("! preserved suffix");
                foreach(int type in new[]{42,45,48}){model.Type=type;model.Save(path,CultureInfo.InvariantCulture);Check(Equal(model,InputSurfaceFile.Load(path,CultureInfo.InvariantCulture)),"Unit35 type "+type+" multi-dimensional round trip and 30 transition branches");}
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(Equal(model,InputSurfaceFile.Load(path,CultureInfo.GetCultureInfo("ru-RU")))&&File.Exists(path+".bak"),"Unit35 comma format and backup");
                byte[] before=File.ReadAllBytes(path);model.Corrections[1].Values[59,99]=double.PositiveInfinity;Reject(()=>model.Save(path),"Unit35 nonfinite last transition value rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit35 validation leaves source intact");model.Corrections[1].Values[59,99]=159.75;
                model.Resize(100,4,3,3);Check(model.Values[1,1,2,99]==1201.625&&model.Values[2,2,3,99]==0,"Unit35 resize preserves intersections and initializes added cells");
                model.Corrections[0].Resize(29,99);Check(model.Corrections[0].Values[57,98]==155.75,"Unit35 transition resize preserves values");
                Reject(()=>model.Resize(101,1,1,1),"Unit35 X capacity enforced");Reject(()=>model.Resize(1,1,31,1),"Unit35 temperature capacity enforced");Reject(()=>model.Resize(1,1,1,51),"Unit35 branch capacity enforced");
                Check(InputSurfaceFile.ParseClipboard("1\t2\n3\t4")[1][1]==4,"Unit35 Excel paste retains last line");Reject(()=>InputSurfaceFile.ParseClipboard(string.Join("\t",Enumerable.Repeat("1",101))),"Unit35 Excel column limit");
                File.WriteAllText(path,"42 incomplete\r\n1 1 1 1 1 1\r\n0 1 1 1\r\n",LegacyCodec.TextEncoding);Reject(()=>InputSurfaceFile.Load(path),"Unit35 invalid dimensions rejected");File.WriteAllText(path,"42 incomplete\r\n",LegacyCodec.TextEncoding);Reject(()=>InputSurfaceFile.Load(path),"Unit35 incomplete file rejected");
                string source=Path.Combine(sourceRoot,"sp-12","data","ResultSTV.DAT");before=File.ReadAllBytes(source);var original=InputSurfaceFile.Load(source,CultureInfo.InvariantCulture);original.Save(path,CultureInfo.InvariantCulture);Check(Equal(original,InputSurfaceFile.Load(path,CultureInfo.InvariantCulture)),"Unit35 ResultSTV original round trip");Check(File.ReadAllLines(path,LegacyCodec.TextEncoding)[3].Contains("(")&&original.TrailingLines.SequenceEqual(InputSurfaceFile.Load(path).TrailingLines),"Unit35 original comments and result tail retained");Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit35 original file unchanged");
                Reject(()=>InputSurfaceFile.Load(Path.Combine(sourceRoot,"sp-12","data","ResultSTN.DAT")),"Unit35 old scalar table explicitly rejected");Reject(()=>InputSurfaceFile.Load(Path.Combine(sourceRoot,"sp-12","data","ResultTVD.DAT")),"Unit35 Unit37 type explicitly rejected");
            }
            finally{Directory.Delete(temp,true);}
        }
        private static bool Equal(InputSurfaceFile a,InputSurfaceFile b)=>a.Type==b.Type&&a.Comment==b.Comment&&a.Scales.SequenceEqual(b.Scales)&&a.X.SequenceEqual(b.X)&&a.Y.SequenceEqual(b.Y)&&a.Temperatures.SequenceEqual(b.Temperatures)&&a.BranchValues.Cast<double>().SequenceEqual(b.BranchValues.Cast<double>())&&a.Values.Cast<double>().SequenceEqual(b.Values.Cast<double>())&&a.Corrections.Zip(b.Corrections,(x,y)=>x.Arguments.SequenceEqual(y.Arguments)&&x.Values.Cast<double>().SequenceEqual(y.Values.Cast<double>())).All(v=>v)&&a.TrailingLines.SequenceEqual(b.TrailingLines);
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
