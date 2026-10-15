using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit24Checks
    {
        public static InputQuadCurveFile Example()
        {
            var model=new InputQuadCurveFile{Comment="Четыре характеристики"};int[] counts={1,3,11,100};int[] parameters={0,260,546,1999};
            for(int group=0;group<4;group++)
            {
                var curve=model.Curves[group];curve.Count=counts[group];curve.Parameter=parameters[group];curve.CountCaption="Количество";curve.ParameterCaption="Параметр";curve.XCaption="X";curve.YCaption="Y";
                for(int row=0;row<2;row++)for(int col=0;col<curve.Count;col++)curve.Values[row,col]=(col+group-row)/100d;
            }
            return model;
        }
        public static void Run()
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit24Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"36.dat");var model=Example();model.Save(path,CultureInfo.InvariantCulture);
                var lines=File.ReadAllLines(path,LegacyCodec.TextEncoding);var saved=InputQuadCurveFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"));
                Check(lines.Length==17&&lines[5].StartsWith("260 ")&&lines[6].StartsWith("3 "),"Unit24 parameter precedes count in all blocks");
                Check(saved.Curves.Select(c=>c.Count).SequenceEqual(new[]{1,3,11,100})&&saved.Curves.Select(c=>c.Parameter).SequenceEqual(new[]{0,260,546,1999}),"Unit24 independent sizes and unknown parameter preserved");
                Check(model.Curves.Zip(saved.Curves,(a,b)=>a.Values.Cast<double>().SequenceEqual(b.Values.Cast<double>())).All(v=>v)&&lines[15].Length==1407,"Unit24 F6 width 14 and 100 points round trip");
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(InputQuadCurveFile.Load(path,CultureInfo.GetCultureInfo("ru-RU")).Curves[3].Values[1,99]==1.01&&File.Exists(path+".bak"),"Unit24 comma round trip and backup");
                byte[] before=File.ReadAllBytes(path);model.Curves[3].Values[1,99]=123456789;Reject(()=>model.Save(path),"Unit24 fourth curve overflow rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit24 fourth curve failure preserves entire file");
                model.Curves[3].Values[1,99]=0;model.Curves[3].Count=101;Reject(()=>model.Save(path),"Unit24 excessive point count rejected");
                File.WriteAllText(path,string.Join("\r\n",lines.Take(16)),LegacyCodec.TextEncoding);Reject(()=>InputQuadCurveFile.Load(path),"Unit24 incomplete fourth block rejected");
                lines[0]="21 Wrong";File.WriteAllLines(path,lines,LegacyCodec.TextEncoding);Reject(()=>InputQuadCurveFile.Load(path),"Unit24 wrong file type rejected");
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
