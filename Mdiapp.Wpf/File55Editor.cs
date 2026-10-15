using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class File55Editor
    {
        public static UIElement Create(ApplicationSettings settings,string path)
        {
            var model=InputFile55.Load(path);var panel=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);var comment=Ui.Text(top,"Комментарий",model.Comment);var tabs=new TabControl();panel.Children.Add(tabs);var commits=new Action[15];
            // Use the six original groups, retaining each curve's physical position in the file.
            int[][] groups={new[]{0,1,2},new[]{3,4,5},new[]{6,7,8},new[]{9},new[]{10,11,12,13},new[]{14}};
            for(int group=0;group<groups.Length;group++)
            {
                var curves=new TabControl();string title=settings.Menu.Get("files","f55tab"+(group+1),"Группа "+(group+1));if(string.IsNullOrWhiteSpace(title))title="Группа "+(group+1);Ui.Tab(tabs,title,curves);
                foreach(int index in groups[group])
                {
                    string caption=settings.Menu.Get("files","f55l"+(index+1),"Характеристика "+(index+1));if(string.IsNullOrWhiteSpace(caption))caption="Характеристика "+(index+1);
                    Ui.Tab(curves,caption,CurveEditor.CreateContent(settings,model.Curves[index],"f55l33","f55l34","f55t"+(index+1),out commits[index],false));
                }
            }
            Action<string> save=target=>{Ui.Commit(panel);foreach(var commit in commits)commit();model.Comment=comment.Text;model.Save(target);path=target;};
            return Ui.Editor(panel,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
