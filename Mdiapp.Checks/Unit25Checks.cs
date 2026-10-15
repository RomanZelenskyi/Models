using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit25Checks
    {
        public static ControllersFile Example()
        {
            var model=new ControllersFile{Type=29,Comment="Тест",HasGains=true,HasAdditionalValues=true};
            for(int category=0;category<3;category++)for(int group=0;group<5;group++)
            {
                var item=new ControllerSetting{Parameter=186+group,Comment="Набор "+category+" группа "+group,Mode=category==0?group%4:group%3,Kp=1.25+group,Ki=0.5};
                for(int part=0;part<5;part++)
                {
                    var value=item.Components[part];value.Mode=part%3;value.Constant=0.25+part;value.Table.Parameter=1999-part;value.Table.Count=30;
                    for(int row=0;row<2;row++)for(int col=0;col<30;col++)value.Table.Values[row,col]=(part+row+col)/100d;
                }
                model.Groups[category,group].Add(item);
            }
            model.AdditionalValues[0]=2;model.AdditionalValues[1]=3;model.AdditionalValues[2]=4;return model;
        }
        private static bool Equal(ControllersFile a,ControllersFile b)
        {
            if(a.Type!=b.Type||!a.AdditionalValues.SequenceEqual(b.AdditionalValues))return false;
            for(int c=0;c<3;c++)for(int g=0;g<5;g++)
            {
                if(a.Groups[c,g].Count!=b.Groups[c,g].Count)return false;
                for(int i=0;i<a.Groups[c,g].Count;i++)
                {
                    var x=a.Groups[c,g][i];var y=b.Groups[c,g][i];if(x.Parameter!=y.Parameter||x.Comment!=y.Comment||x.Mode!=y.Mode||x.Kp!=y.Kp||x.Ki!=y.Ki)return false;
                    for(int p=0;p<(x.Mode==3?5:x.Mode==2?3:1);p++)
                    {
                        int mode=x.Mode<2?x.Mode:x.Components[p].Mode;var v=x.Components[p];var w=y.Components[p];
                        if(mode!=w.Mode||mode==0&&v.Constant!=w.Constant||mode==2&&v.Table.Parameter!=w.Table.Parameter)return false;
                        if(mode==1&&(v.Table.Count!=w.Table.Count||v.Table.Parameter!=w.Table.Parameter||!v.Table.Values.Cast<double>().SequenceEqual(w.Table.Values.Cast<double>())))return false;
                    }
                }
            }
            return true;
        }
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit25Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string path=Path.Combine(temp,"controllers.dat");var model=Example();model.Save(path,CultureInfo.InvariantCulture);var saved=ControllersFile.Load(path,CultureInfo.GetCultureInfo("ru-RU"));
                Check(Equal(model,saved),"Unit25 all categories five groups modes 0/1/2/3 round trip");
                Check(saved.Groups[2,1][0].Comment=="Набор 2 группа 1"&&saved.Groups[0,3][0].Components[3].Constant==3.25,"Unit25 second additional comments and fourth component preserved");
                model.Save(path,CultureInfo.GetCultureInfo("ru-RU"));Check(Equal(model,ControllersFile.Load(path,CultureInfo.GetCultureInfo("ru-RU")))&&File.Exists(path+".bak"),"Unit25 comma round trip gains additional values and backup");
                byte[] before=File.ReadAllBytes(path);model.Groups[0,0][0].Components[0].Constant=double.NaN;Reject(()=>model.Save(path),"Unit25 nonfinite constant rejected");Check(before.SequenceEqual(File.ReadAllBytes(path)),"Unit25 invalid save preserves file");
                model=Example();model.Groups[0,1][0].Components[0].Table.Count=31;Reject(()=>model.Save(path),"Unit25 more than 30 table points rejected");
                model=Example();for(int i=0;i<5;i++)model.Groups[1,0].Add(new ControllerSetting());Reject(()=>model.Save(path),"Unit25 additional group capacity enforced");
                File.WriteAllText(path,"40 Short\r\n0\r\n",LegacyCodec.TextEncoding);Reject(()=>ControllersFile.Load(path),"Unit25 truncated groups rejected");
                if(sourceRoot!=null)foreach(string name in new[]{"Controllers.DAT","Controllers_1.dat"})
                {
                    string source=Path.Combine(sourceRoot,"sp-12","data",name);before=File.ReadAllBytes(source);var real=ControllersFile.Load(source,CultureInfo.InvariantCulture);real.Save(path,CultureInfo.InvariantCulture);var copy=ControllersFile.Load(path,CultureInfo.InvariantCulture);
                    Check(Equal(real,copy)&&real.HasGains==copy.HasGains&&real.HasAdditionalValues==copy.HasAdditionalValues,"Unit25 original round trip "+name);
                    Check(before.SequenceEqual(File.ReadAllBytes(source)),"Unit25 original unchanged "+name);
                }
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Reject(Action action,string name){try{action();}catch(InvalidDataException){Check(true,name);return;}throw new Exception(name);}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
