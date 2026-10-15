using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit16Checks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit16Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                var pasted=CharacteristicFile.ParseClipboardRows("1,25\t2.5\r\n3\tbad\r\n4\t5\r\n6\t7",CultureInfo.GetCultureInfo("ru-RU"));
                Check(pasted.Length==3&&pasted[0].SequenceEqual(new[]{1.25,2.5})&&pasted[1][1]==0,"Unit16 Excel clipboard rows cultures and zero fallback");
                var model=new CharacteristicFile{Type=4,Comment="Проверка",Interpolation=-10,AdditionalValue=1.2};
                for(int i=0;i<4;i++)model.Coefficients[i]=i+0.25;
                foreach(int count in new[]{11,3,99})
                {
                    var branch=new CharacteristicBranch{Value=count/10d,Points=new double[3,count]};
                    for(int row=0;row<3;row++)for(int col=0;col<count;col++)branch.Points[row,col]=(row+col)/100d;
                    model.Branches.Add(branch);
                }
                string path=Path.Combine(temp,"branch.dat");model.Save(path,CultureInfo.InvariantCulture);
                var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);
                Check(lines[3]=="  3 99"&&lines[4]=="    1.111"&&lines[5].Length==70&&lines[8].Length==7,"Unit16 fixed fields and ten-point blocks");
                var copy=CharacteristicFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"));
                Check(copy.Branches.Select(b=>b.Points.GetLength(1)).SequenceEqual(new[]{11,3,99})&&copy.Branches[2].Points[2,98]==1,"Unit16 variable branch counts and 99 points");
                Check(copy.Interpolation==-10&&copy.AdditionalValue==1.2&&copy.Coefficients.SequenceEqual(model.Coefficients),"Unit16 coefficients and interpolation");
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));
                Check(CharacteristicFile.Load(path,CultureInfo.GetCultureInfo("ru-RU")).Branches[0].Points[1,10]==0.11&&File.Exists(path+".bak"),"Unit16 comma round trip and backup");
                var before=File.ReadAllBytes(path);model.Branches[0].Points[0,0]=123456789;
                Reject(()=>model.Save(path),"Unit16 overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit16 invalid save leaves original unchanged");
                model.Branches[0].Points=new double[3,100];Reject(()=>model.Save(path),"Unit16 100 points rejected before count truncation");
                File.WriteAllText(path,"4 Short\r\n1 1 1 1\r\n  10   0.0\r\n  1 11\r\n    1.011\r\n",LegacyCodec.TextEncoding);Reject(()=>CharacteristicFile.Load(path),"Unit16 truncated branch rejected");
                if(sourceRoot!=null)
                {
                    var files=Directory.GetFiles(Path.Combine(sourceRoot,"sp-12","data")).Where(p=>new[]{"Xknd1.dat","Xknd2.dat","Xtvd.dat","Xtvd-cor.dat","Xtnd.dat","Xtnd1.dat","Xtnd1_1.dat","xtnd1_2.dat","xtnd1_3.dat","xtnd1_4.dat","xtnd1_5.dat"}.Contains(Path.GetFileName(p),StringComparer.OrdinalIgnoreCase)).ToArray();
                    foreach(string source in files)
                    {
                        byte[] original=File.ReadAllBytes(source);var real=CharacteristicFile.Load(source,CultureInfo.InvariantCulture);real.Save(path,CultureInfo.InvariantCulture);var saved=CharacteristicFile.Load(path,CultureInfo.InvariantCulture);
                        Check(real.Type==saved.Type&&real.Branches.Count==saved.Branches.Count&&real.Branches.Zip(saved.Branches,(a,b)=>a.Value==b.Value&&a.Points.Cast<double>().SequenceEqual(b.Points.Cast<double>())).All(v=>v)&&real.SpecialSuffix==saved.SpecialSuffix&&real.ControlSuffix==saved.ControlSuffix,"Unit16 real round trip "+Path.GetFileName(source));
                        Check(original.SequenceEqual(File.ReadAllBytes(source)),"Unit16 source unchanged "+Path.GetFileName(source));
                    }
                    Check(files.Length==11,"Unit16 all supplied branch characteristics checked");
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
