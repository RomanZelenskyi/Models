using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit18Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit18Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"curve.dat");
                foreach(int type in new[]{5,8,18,26})
                {
                    var model=new InputCurveFile{Type=type,Count=100,Parameter=1999,Comment="Unit18",XCaption="X",YCaption="Y"};
                    for(int i=0;i<100;i++){model.Values[0,i]=i/10d;model.Values[1,i]=-i/100d;}
                    model.Save(path,CultureInfo.InvariantCulture);var loaded=InputCurveFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"));
                    Check(loaded.Type==type&&loaded.Parameter==1999&&loaded.Values.Cast<double>().SequenceEqual(model.Values.Cast<double>()),"Unit18 type "+type+" 100 points and sparse ID round trip");
                }
                if(sourceRoot!=null)foreach(string name in new[]{"XPER1.dat","XPER4.dat"})
                {
                    string source=Path.Combine(sourceRoot,"sp-12","data",name);byte[] original=File.ReadAllBytes(source);var model=InputCurveFile.Load(source,CultureInfo.InvariantCulture);model.Save(path,CultureInfo.InvariantCulture);
                    Check(original.SequenceEqual(File.ReadAllBytes(path)),"Unit18 original byte exact round trip "+name);
                    model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));var loaded=InputCurveFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"));
                    Check(loaded.Type==model.Type&&loaded.Parameter==model.Parameter&&loaded.Values.Cast<double>().SequenceEqual(model.Values.Cast<double>()),"Unit18 comma round trip "+name);
                    Check(original.SequenceEqual(File.ReadAllBytes(source)),"Unit18 source unchanged "+name);
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
