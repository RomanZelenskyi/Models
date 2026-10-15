using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class DoubleCurveEditor
    {
        public static UIElement Create(ApplicationSettings settings,string path)
        {
            var model=InputDoubleCurveFile.Load(path);var panel=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);
            var comment=Ui.Text(top,"Комментарий",model.Comment);var tabs=new TabControl();panel.Children.Add(tabs);
            var commits=new Action[2];
            for(int i=0;i<2;i++)Ui.Tab(tabs,"Характеристика "+(i+1),CurveEditor.CreateContent(settings,model.Curves[i],"n"+(51+i*3),"n"+(52+i*3),"n"+(53+i*3),out commits[i],false));
            var constants=new StackPanel();Ui.Tab(tabs,"Дополнительные значения",constants);var fields=new TextBox[3];
            for(int i=0;i<3;i++)fields[i]=Ui.Text(constants,settings.Menu.Get("files","n"+(57+i),"Значение "+(i+1)),model.Constants[i].ToString(CultureInfo.CurrentCulture));
            Action<string> save=target=>{Ui.Commit(panel);foreach(var commit in commits)commit();model.Comment=comment.Text;for(int i=0;i<3;i++)model.Constants[i]=InputFiles3132.Parse(fields[i].Text,CultureInfo.CurrentCulture);model.Save(target);path=target;};
            return Ui.Editor(panel,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
