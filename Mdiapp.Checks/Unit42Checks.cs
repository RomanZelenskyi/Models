using System;
using System.IO;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit42Checks
    {
        public static void Run()
        {
            string temp=Path.Combine(Path.GetTempPath(),"Unit42Checks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                string data=Path.Combine(temp,"data"),prefs=Path.Combine(temp,"prefs","models.ini");Directory.CreateDirectory(Path.Combine(data,"Ru"));File.WriteAllText(Path.Combine(data,"models.ini"),"[Interface]\r\nlang=Ru\r\nnew=notice.txt\r\n[Engine]\r\nname=sample\r\ncode=3\r\n",LegacyCodec.TextEncoding);
                var settings=new ApplicationSettings(data,prefs);Check(settings.ShowUpdateNotice&&settings.UpdateNoticePath==Path.Combine(data,"Ru","notice.txt"),"Unit42 default startup visibility and language-specific file");settings.SetShowUpdateNotice(false);Check(!settings.ShowUpdateNotice&&IniFile.Load(prefs).Get("Engine","name")=="sample","Unit42 disabling notice preserves other preferences");settings.SetShowUpdateNotice(true);Check(new ApplicationSettings(data,prefs).ShowUpdateNotice&&File.Exists(prefs+".bak"),"Unit42 preference persists and backup is created");
                var ini=IniFile.Load(Path.Combine(data,"models.ini"));ini.Set("Interface","new","");ini.Save(Path.Combine(data,"models.ini"));Check(settings.UpdateNoticePath==null,"Unit42 empty document setting supported");ini.Set("Interface","new","..\\..\\outside.txt");ini.Save(Path.Combine(data,"models.ini"));try{var path=settings.UpdateNoticePath;throw new Exception("Unit42 invalid document path accepted");}catch(InvalidDataException){Check(true,"Unit42 document stays within language directory");}
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
