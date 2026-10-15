using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit26Checks
    {
        public static MultiCharacteristicFile Example()
        {
            var model=new MultiCharacteristicFile{Comment="Характеристики 1500"};
            for(int c=0;c<2;c++)
            {
                var item=new MultiCharacteristic{Mach=0.25+c};
                for(int b=0;b<2;b++){var branch=new MultiCharacteristicBranch{Value=10+c*2+b};branch.Curve.Count=b==0?3:100;branch.Curve.XCaption="X";branch.Curve.YCaption="Y";for(int row=0;row<2;row++)for(int p=0;p<branch.Curve.Count;p++)branch.Curve.Values[row,p]=(c+b+row+p)/100d;item.Branches.Add(branch);}
                model.Characteristics.Add(item);
            }
            return model;
        }
        public static void Run()
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit26Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"1500.dat");var model=Example();model.Save(path,CultureInfo.InvariantCulture);var lines=File.ReadAllLines(path);var copy=MultiCharacteristicFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"));
                Check(lines[2].StartsWith("  2 100    0.250000")&&lines[3].StartsWith("   10.000000   3")&&lines[7].Length==1212,"Unit26 fixed headers and 12-wide F6 point fields");
                Check(copy.Characteristics.Count==2&&copy.Characteristics[1].Mach==1.25&&copy.Characteristics[0].Branches[1].Curve.Count==100&&copy.Characteristics[1].Branches[1].Curve.Values[1,99]==1.02,"Unit26 multiple characteristics variable counts 100 points");
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(MultiCharacteristicFile.Load(path,CultureInfo.GetCultureInfo("ru-RU")).Characteristics[0].Mach==0.25&&File.Exists(path+".bak"),"Unit26 decimal comma and backup");
                byte[] before=File.ReadAllBytes(path);model.Characteristics[1].Branches[1].Curve.Values[1,99]=123456789;Reject(()=>model.Save(path),"Unit26 late point overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit26 invalid save preserves original");
                model=Example();model.Characteristics[0].Branches[0].Curve.Count=101;Reject(()=>model.Save(path),"Unit26 point limit enforced");
                model=Example();for(int i=0;i<29;i++)model.Characteristics.Add(new MultiCharacteristic());Reject(()=>model.Save(path),"Unit26 characteristic limit enforced");
                model=Example();for(int i=0;i<29;i++)model.Characteristics[0].Branches.Add(new MultiCharacteristicBranch());Reject(()=>model.Save(path),"Unit26 branch limit enforced");
                File.WriteAllText(path,"1500 Test\r\n1\r\n1 2 0.5\r\n10 2\r\n0 1\r\n",LegacyCodec.TextEncoding);Reject(()=>MultiCharacteristicFile.Load(path),"Unit26 truncated branch rejected");
                File.WriteAllText(path,"1500 Test\r\n1\r\n1 2 0.5\r\n10 2\r\n0\t1\r\n2\t3\r\n",LegacyCodec.TextEncoding);Check(MultiCharacteristicFile.Load(path).Characteristics[0].Branches[0].Curve.Values[1,1]==3,"Unit26 whitespace input supported");
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
