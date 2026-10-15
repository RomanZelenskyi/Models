using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit37Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit37Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"rotor.dat");var model=new InputSurfaceFile{Type=44,Comment="Проверка"};model.Resize(100,3,30,1);model.Corrections[0].Resize(30,100);
                for(int x=0;x<100;x++){model.X[x]=x+.125;model.Corrections[0].Arguments[x]=x+.25;for(int row=0;row<60;row++)model.Corrections[0].Values[row,x]=row+x+.75;}for(int y=0;y<3;y++)model.Y[y]=y+.5;
                for(int t=0;t<30;t++){model.Temperatures[t]=400+t*100;for(int y=0;y<3;y++)for(int x=0;x<100;x++)model.Values[t,0,y,x]=t*1000+y*100+x+.625;}model.TrailingLines.Add("! retained results");
                foreach(int type in new[]{44,47,50}){model.Type=type;model.Save(path,CultureInfo.InvariantCulture);Check(Equal(model,InputSurfaceFile.Load(path,CultureInfo.InvariantCulture,true)),"Unit37 type "+type+" 100 points 30 temperatures and transitions round trip");}
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(Equal(model,InputSurfaceFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"),true))&&File.Exists(path+".bak"),"Unit37 comma culture and backup");
                var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);Check(lines[1].Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries).Length==4&&lines[2].Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries).Length==3,"Unit37 serializes four scales and three dimensions");
                byte[] before=File.ReadAllBytes(path);model.Values[29,0,2,99]=double.NaN;Reject(()=>model.Save(path),"Unit37 nonfinite last temperature rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit37 failed save leaves file intact");model.Values[29,0,2,99]=29299.625;
                model.Resize(100,100,30,1);Check(model.Values[29,0,2,99]==29299.625&&model.Values[29,0,99,99]==0,"Unit37 Y resize to 100 preserves existing values");
                Reject(()=>model.Resize(1,1,1,2),"Unit37 extra surface branches rejected");Reject(()=>model.Resize(1,1,31,1),"Unit37 temperature limit enforced");Reject(()=>model.Resize(1,101,1,1),"Unit37 Y limit enforced");
                File.WriteAllText(path,"44 incomplete\r\n1 1 1 1\r\n1 1 1\r\n",LegacyCodec.TextEncoding);Reject(()=>InputSurfaceFile.Load(path,unit37:true),"Unit37 incomplete matrix rejected");
                string source=Path.Combine(sourceRoot,"sp-12","data","ResultTVD.DAT");before=File.ReadAllBytes(source);var original=InputSurfaceFile.Load(source,CultureInfo.InvariantCulture,true);original.Save(path,CultureInfo.InvariantCulture);var saved=InputSurfaceFile.Load(path,CultureInfo.InvariantCulture,true);
                Check(original.Type==44&&original.X.Length==6&&original.Y.Length==5&&original.Temperatures.Length==6&&original.BranchCount==1,"Unit37 ResultTVD dimensions");Check(Equal(original,saved),"Unit37 ResultTVD original round trip");Check(saved.TrailingLines.Count>0&&File.ReadAllLines(path,LegacyCodec.TextEncoding)[1].Contains("!")&&original.TrailingLines.SequenceEqual(saved.TrailingLines),"Unit37 comments and original result tail retained");Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit37 original unchanged");
                Reject(()=>InputSurfaceFile.Load(Path.Combine(sourceRoot,"sp-12","data","ResultTND.DAT"),unit37:true),"Unit37 old Unit22 format rejected");Reject(()=>InputSurfaceFile.Load(source),"Unit35 and Unit37 editors remain distinct");Reject(()=>InputSurfaceFile.Load(Path.Combine(sourceRoot,"sp-12","data","ResultSTV.DAT"),unit37:true),"Unit37 Unit35 format rejected");
            }
            finally{Directory.Delete(temp,true);}
        }
        private static bool Equal(InputSurfaceFile a,InputSurfaceFile b)=>a.Type==b.Type&&a.Comment==b.Comment&&a.Scales.Take(4).SequenceEqual(b.Scales.Take(4))&&a.X.SequenceEqual(b.X)&&a.Y.SequenceEqual(b.Y)&&a.Temperatures.SequenceEqual(b.Temperatures)&&a.Values.Cast<double>().SequenceEqual(b.Values.Cast<double>())&&a.Corrections[0].Arguments.SequenceEqual(b.Corrections[0].Arguments)&&a.Corrections[0].Values.Cast<double>().SequenceEqual(b.Corrections[0].Values.Cast<double>())&&a.TrailingLines.SequenceEqual(b.TrailingLines);
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
