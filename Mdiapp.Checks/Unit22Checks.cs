using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit22Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit22Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"three.dat");
                var model=new InputThreeColumnFile{InitialValue=1433,Comment="Тест"};for(int i=0;i<1000;i++)model.Rows.Add(new[]{(double)i,i/10000d,-i/10000d});
                foreach(int type in new[]{42,44,45,47,48,50})
                {
                    model.Type=type;model.Save(path,CultureInfo.InvariantCulture);var copy=InputThreeColumnFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"));
                    Check(copy.Type==type&&copy.Rows.Count==1000&&copy.Rows[999].SequenceEqual(model.Rows[999]),"Unit22 type "+type+" 1000 rows round trip");
                }
                var lines=File.ReadAllLines(path);Check(lines[1]=="  1433.00"&&lines[2].Length==72,"Unit22 initial F2 width 9 and three F14 fields width 24");
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(InputThreeColumnFile.Load(path,CultureInfo.GetCultureInfo("ru-RU")).Rows[999][1]==0.0999&&File.Exists(path+".bak"),"Unit22 comma round trip and backup");
                byte[] before=File.ReadAllBytes(path);model.Rows[999][2]=double.NaN;Reject(()=>model.Save(path),"Unit22 nonfinite last row rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit22 last row failure preserves original");
                model.Rows[999][2]=1234567890;Reject(()=>model.Save(path),"Unit22 field overflow rejected");model.Rows.Add(new double[3]);Reject(()=>model.Save(path),"Unit22 more than 1000 rows rejected");
                var paste=InputThreeColumnFile.ParseClipboardRows("1,25\t2.5\t3\r\n4\t5\t6",CultureInfo.GetCultureInfo("ru-RU"));Check(paste.Length==2&&paste[0][0]==1.25&&paste[1][2]==6,"Unit22 Excel retains last row without trailing newline");
                Reject(()=>InputThreeColumnFile.ParseClipboardRows("1\t2\t3\t4"),"Unit22 excess clipboard columns rejected");
                if(sourceRoot!=null)
                {
                    foreach(string name in new[]{"ResultSTN.DAT","ResultTND.DAT"})
                    {
                        string source=Path.Combine(sourceRoot,"sp-12","data",name);before=File.ReadAllBytes(source);var real=InputThreeColumnFile.Load(source,CultureInfo.InvariantCulture);real.Save(path,CultureInfo.InvariantCulture);var copy=InputThreeColumnFile.Load(path,CultureInfo.InvariantCulture);
                        Check(real.Type==copy.Type&&real.InitialValue==copy.InitialValue&&real.Rows.Count==copy.Rows.Count&&real.Rows.Zip(copy.Rows,(a,b)=>a.SequenceEqual(b)).All(v=>v),"Unit22 original round trip "+name);
                        Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit22 source unchanged "+name);
                    }
                    Reject(()=>InputThreeColumnFile.Load(Path.Combine(sourceRoot,"sp-12","data","ResultSTV.DAT")),"Unit22 incompatible original result layout rejected");
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
