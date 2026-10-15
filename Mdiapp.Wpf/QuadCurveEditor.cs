using System;
using System.Windows;
using System.Windows.Controls;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class QuadCurveEditor
    {
        public static UIElement Create(ApplicationSettings settings,string path)
        {
            var model=InputQuadCurveFile.Load(path);var panel=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);
            var comment=Ui.Text(top,"Комментарий",model.Comment);var tabs=new TabControl();panel.Children.Add(tabs);var commits=new Action[4];
            for(int i=0;i<4;i++)Ui.Tab(tabs,"Характеристика "+(i+1),CurveEditor.CreateContent(settings,model.Curves[i],"n86","n"+(87+i*2),"n"+(88+i*2),out commits[i],false));
            Action<string> save=target=>{Ui.Commit(panel);foreach(var commit in commits)commit();model.Comment=comment.Text;model.Save(target);path=target;};
            return Ui.Editor(panel,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
