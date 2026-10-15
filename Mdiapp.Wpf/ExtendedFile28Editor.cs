using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class ExtendedFile28Editor
    {
        public static UIElement Create(ApplicationSettings settings,string path)
        {
            var definitions=IniFile.Load(Path.Combine(settings.DataDirectory,settings.Engine,settings.Language,"inputfiles.ini"));var model=InputExtendedFile28.Load(path,definitions);var tabs=new TabControl();
            Ui.Tab(tabs,"Таблица отборов",Editors.File28Content(model.Data,out Action commit));var coefficients=new StackPanel();Ui.Tab(tabs,"Коэффициенты",coefficients);
            var fields=model.Coefficients.Select((value,i)=>Ui.Text(coefficients,"Коэффициент "+(i+1),value.ToString(CultureInfo.CurrentCulture))).ToArray();for(int i=0;i<3;i++)fields[i].Name="Coefficient32_"+i;
            Action<string> save=target=>{commit();for(int i=0;i<3;i++)model.Coefficients[i]=InputFiles3132.Parse(fields[i].Text,CultureInfo.CurrentCulture);model.Save(target);path=target;};
            return Ui.Editor(tabs,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
