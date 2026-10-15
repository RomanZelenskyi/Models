using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit32Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit32Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                var definitions=IniFile.Load(Path.Combine(sourceRoot,"sp-12","Ru","inputfiles.ini"));string path=Path.Combine(temp,"28.dat");
                foreach(string name in new[]{"Xotbor1.dat","Xotbor2.dat","Xotbor3.dat"})
                {
                    string source=Path.Combine(sourceRoot,"sp-12","data",name);byte[] before=File.ReadAllBytes(source);var model=InputExtendedFile28.Load(source,definitions,CultureInfo.InvariantCulture);
                    Check(model.Coefficients.SequenceEqual(new[]{0.2823,0.9292,0.00729}),"Unit32 original coefficients "+name);
                    model.Save(path,CultureInfo.InvariantCulture);var saved=InputExtendedFile28.Load(path,definitions,CultureInfo.InvariantCulture);
                    Check(saved.Data.Type==model.Data.Type&&saved.Coefficients.SequenceEqual(model.Coefficients)&&saved.Data.Rows.All(r=>saved.Data.Columns.All(c=>saved.Data.Values[r.PhysicalIndex,c.PhysicalIndex]==model.Data.Values[r.PhysicalIndex,c.PhysicalIndex]))&&saved.Data.TrailingLines.Skip(1).SequenceEqual(model.Data.TrailingLines.Skip(1)),"Unit32 matrix type coefficients and extra lines round trip "+name);
                    Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit32 original unchanged "+name);
                }
                var edited=InputExtendedFile28.Load(path,definitions);edited.Coefficients[2]=0.125;edited.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(InputExtendedFile28.Load(path,definitions,CultureInfo.GetCultureInfo("ru-RU")).Coefficients[2]==0.125&&File.Exists(path+".bak"),"Unit32 comma save coefficient and backup");
                byte[] snapshot=File.ReadAllBytes(path);edited.Coefficients[2]=double.NaN;Reject(()=>edited.Save(path),"Unit32 nonfinite coefficient rejected");Check(snapshot.SequenceEqual(File.ReadAllBytes(path)),"Unit32 invalid coefficient preserves file");
                var extended=new IniFile();extended.Set("28","n28count","12");extended.Set("28","ce1","1");extended.Set("28","nrow1","1");extended.Set("28","re12","1");
                var lines=new System.Collections.Generic.List<string>{"281 Extended",new string(' ',135)};for(int i=0;i<12;i++)lines.Add(string.Concat(Enumerable.Range(0,15).Select(c=>"  0.12000")));lines.Add(string.Concat(Enumerable.Repeat("        0.00000",3)));lines.Add("1 2 3");File.WriteAllLines(path,lines,LegacyCodec.TextEncoding);
                InputExtendedFile28.Load(path,extended).Save(path,CultureInfo.InvariantCulture);Check(InputExtendedFile28.Load(path,extended).Data.Values[11,0]==0.12,"Unit32 enabled row after tenth survives save");
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
