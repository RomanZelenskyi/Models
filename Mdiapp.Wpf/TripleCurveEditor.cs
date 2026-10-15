using System;
using System.Windows;
using System.Windows.Controls;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class TripleCurveEditor
    {
        public static UIElement Create(ApplicationSettings settings,string path)
        {
            var model=InputTripleCurveFile.Load(path);var panel=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);
            var comment=Ui.Text(top,"Комментарий",model.Comment);var tabs=new TabControl();panel.Children.Add(tabs);var commits=new Action[3];
            for(int i=0;i<3;i++)Ui.Tab(tabs,"Характеристика "+(i+1),CurveEditor.CreateContent(settings,model.Curves[i],"n"+(60+i*3),"n"+(61+i*3),"n"+(62+i*3),out commits[i],false));
            Action<string> save=target=>{Ui.Commit(panel);foreach(var commit in commits)commit();model.Comment=comment.Text;model.Save(target);path=target;};
            return Ui.Editor(panel,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
