using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit23Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit23Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"23.dat");var model=new InputThreeColumnFile();
                model.InitialValues[0]=1378.89;model.InitialValues[1]=1.125;model.InitialValues[2]=40;
                for(int i=0;i<1000;i++)model.Rows.Add(new[]{i/2d,i/1000d,-i/1000d});
                foreach(int type in new[]{43,46,49})
                {
                    model.Type=type;model.Save(path,CultureInfo.InvariantCulture);var copy=InputThreeColumnFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"));
                    Check(copy.IsUnit23&&copy.Type==type&&copy.InitialValues.SequenceEqual(model.InitialValues)&&copy.Rows.Count==1000&&copy.Rows[999].SequenceEqual(model.Rows[999]),"Unit23 type "+type+" three initial values and 1000 rows");
                }
                Check(File.ReadAllLines(path)[1]=="  1378.89  1.125  40","Unit23 initial values use Delphi general format");
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(InputThreeColumnFile.Load(path,CultureInfo.GetCultureInfo("ru-RU")).InitialValues.SequenceEqual(model.InitialValues)&&File.Exists(path+".bak"),"Unit23 comma round trip and backup");
                byte[] before=File.ReadAllBytes(path);model.InitialValues[2]=double.PositiveInfinity;Reject(()=>model.Save(path),"Unit23 nonfinite third initial value rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit23 initial validation preserves file");
                File.WriteAllText(path,"49 Test\r\n1 2\r\n0 0 0\r\n",LegacyCodec.TextEncoding);Reject(()=>InputThreeColumnFile.Load(path),"Unit23 missing third initial value rejected");
                if(sourceRoot!=null)
                {
                    string source=Path.Combine(sourceRoot,"sp-12","data","ResultTNDBL.dat");before=File.ReadAllBytes(source);var real=InputThreeColumnFile.Load(source,CultureInfo.InvariantCulture);real.Save(path,CultureInfo.InvariantCulture);var copy=InputThreeColumnFile.Load(path,CultureInfo.InvariantCulture);
                    Check(real.Type==49&&real.InitialValues.SequenceEqual(new[]{1378.89,1,40})&&real.Rows.Count==copy.Rows.Count&&real.Rows.Zip(copy.Rows,(a,b)=>a.SequenceEqual(b)).All(v=>v),"Unit23 original ResultTNDBL round trip");
                    Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit23 original unchanged");
                    Reject(()=>InputThreeColumnFile.Load(Path.Combine(sourceRoot,"sp-12","data","ResultTVDBL.dat")),"Unit23 incompatible multidimensional results rejected");
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
