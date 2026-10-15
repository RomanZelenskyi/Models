using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class ControllersEditor
    {
        private static ComboBox Parameter(Panel panel,string title,int number,Unit3Model catalog,ApplicationSettings settings)
        {
            var choices=catalog.Parameters.Where(p=>!string.IsNullOrEmpty(p.Symbol)&&settings.IsParameterActive(p.Number)).Select(p=>new Editors.ParameterOption{Number=p.Number,Name=p.Symbol+" "+p.Name}).ToList();choices.Insert(0,new Editors.ParameterOption{Number=0,Name="Без параметра"});
            if(!choices.Any(p=>p.Number==number))choices.Add(new Editors.ParameterOption{Number=number,Name="Из файла"});var combo=Ui.Choices(panel,title,choices);combo.SelectedItem=choices.Single(p=>p.Number==number);return combo;
        }
        public static UIElement Create(ApplicationSettings settings,string path)
        {
            var model=ControllersFile.Load(path);var catalog=new Unit3Model(settings.DataDirectory,settings.Language,settings.Engine);var menu=settings.Menu;
            var dock=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);dock.Children.Add(top);var comment=Ui.Text(top,"Комментарий файла",model.Comment);
            var navigation=new StackPanel{Width=270};DockPanel.SetDock(navigation,Dock.Left);dock.Children.Add(navigation);var detail=new ContentControl();dock.Children.Add(detail);
            var group=Ui.Choices(navigation,"Группа",Enumerable.Range(0,5).Select(i=>menu.Get("pages","N"+(17+i),"Группа "+(i+1))).ToArray());group.Name="ControllerGroup";group.SelectedIndex=0;
            var category=Ui.Choices(navigation,"Набор",new[]{"Основной","Дополнительный 1","Дополнительный 2"});category.Name="ControllerCategory";category.SelectedIndex=0;
            var list=new ListBox{Height=300,Margin=new Thickness(8),Name="ControllerList"};navigation.Children.Add(list);
            Action current=()=>{};bool updating=false;int activeGroup=0,activeCategory=0;
            Action<ControllerSetting> render=null;
            render=item=>
            {
                var body=new DockPanel();var header=new StackPanel();DockPanel.SetDock(header,Dock.Top);body.Children.Add(header);
                var parameter=Parameter(header,"Регулируемый параметр",item.Parameter,catalog,settings);var name=Ui.Text(header,"Комментарий регулятора",item.Comment);name.Name="ControllerComment";
                var mode=Ui.Choices(header,"Вид регулятора",activeCategory==0?new[]{"Константа","Таблица",menu.Get("files","n106a","(Y1+Y2)*Y3"),"Расширенный (5 компонентов)"}:new[]{"Константа","Таблица",menu.Get("files","n106a","(Y1+Y2)*Y3")});mode.SelectedIndex=item.Mode;
                var gains=new StackPanel{Orientation=Orientation.Horizontal};header.Children.Add(gains);var kp=Ui.Text(gains,"Kp",item.Kp.ToString(CultureInfo.CurrentCulture));var ki=Ui.Text(gains,"Ki",item.Ki.ToString(CultureInfo.CurrentCulture));
                var parts=new TabControl();body.Children.Add(parts);int partCount=item.Mode==3?5:item.Mode==2?3:1;var commits=new Action[partCount];
                string[] labels={menu.Get("files","n114","Компонент 1"),menu.Get("files","n115","Компонент 2"),menu.Get("files","n116","Компонент 3"),"Компонент 4","Компонент 5"};
                for(int i=0;i<partCount;i++)
                {
                    int index=i;var part=item.Components[i];int partMode=item.Mode<2?item.Mode:part.Mode;
                    var container=new DockPanel();var controls=new StackPanel();DockPanel.SetDock(controls,Dock.Top);container.Children.Add(controls);
                    var kind=Ui.Choices(controls,"Вид компонента",new[]{"Константа","Таблица","Значение параметра"});kind.SelectedIndex=partMode;kind.IsEnabled=item.Mode>=2;
                    var constantPanel=new StackPanel();controls.Children.Add(constantPanel);var constant=Ui.Text(constantPanel,"Константа",part.Constant.ToString(CultureInfo.CurrentCulture));constant.Name="ControllerConstant"+i;
                    var referencePanel=new StackPanel();controls.Children.Add(referencePanel);var reference=Parameter(referencePanel,"Параметр",part.Table.Parameter,catalog,settings);
                    var curve=CurveEditor.CreateContent(settings,part.Table,"n108","n109","n119",out Action commitCurve,false,30,140);container.Children.Add(curve);
                    Action refresh=()=>{constantPanel.Visibility=kind.SelectedIndex==0?Visibility.Visible:Visibility.Collapsed;referencePanel.Visibility=kind.SelectedIndex==2?Visibility.Visible:Visibility.Collapsed;curve.Visibility=kind.SelectedIndex==1?Visibility.Visible:Visibility.Collapsed;};refresh();
                    commits[i]=()=>{if(partMode==0)part.Constant=InputFiles3132.Parse(constant.Text,CultureInfo.CurrentCulture);else if(partMode==1)commitCurve();else part.Table.Parameter=((Editors.ParameterOption)reference.SelectedItem).Number;part.Mode=partMode;};
                    kind.SelectionChanged+=(s,e)=>{if(e.Source!=kind)return;Ui.Run(()=>{commits[index]();partMode=kind.SelectedIndex;part.Mode=partMode;refresh();});};
                    Ui.Tab(parts,item.Mode<2?"Значение":labels[i],container);
                }
                current=()=>{Ui.Commit(body);item.Parameter=((Editors.ParameterOption)parameter.SelectedItem).Number;item.Comment=name.Text;item.Kp=InputFiles3132.Parse(kp.Text,CultureInfo.CurrentCulture);item.Ki=InputFiles3132.Parse(ki.Text,CultureInfo.CurrentCulture);foreach(var commit in commits)commit();};
                mode.SelectionChanged+=(s,e)=>{if(e.Source!=mode)return;Ui.Run(()=>{current();item.Mode=mode.SelectedIndex;render(item);});};detail.Content=body;
            };
            Action reload=()=>
            {
                current();current=()=>{};activeGroup=group.SelectedIndex;activeCategory=category.SelectedIndex;updating=true;list.ItemsSource=null;list.ItemsSource=model.Groups[activeCategory,activeGroup];list.SelectedIndex=list.Items.Count>0?0:-1;updating=false;
                string flag=catalog.InputFiles.Get("2940","n"+(activeCategory+1)+(activeGroup+1));bool enabled=flag=="1"||bool.TryParse(flag,out bool yes)&&yes;
                detail.IsEnabled=enabled;list.IsEnabled=enabled;detail.Content=null;if(list.SelectedItem is ControllerSetting selected)render(selected);
            };
            list.SelectionChanged+=(s,e)=>{if(updating)return;Ui.Run(()=>{current();current=()=>{};if(list.SelectedItem is ControllerSetting selected)render(selected);else detail.Content=null;});};
            group.SelectionChanged+=(s,e)=>{if(e.Source==group)Ui.Run(reload);};category.SelectionChanged+=(s,e)=>{if(e.Source==category)Ui.Run(reload);};
            navigation.Children.Add(Ui.Button("Добавить регулятор",()=>{current();if(!list.IsEnabled)throw new InvalidOperationException("Этот набор отключён в настройках модели Unit3.");var items=model.Groups[activeCategory,activeGroup];if(items.Count>=(activeCategory==0?20:5))throw new InvalidOperationException("Достигнут предел количества регуляторов.");items.Add(new ControllerSetting());reload();list.SelectedIndex=items.Count-1;}));
            navigation.Children.Add(Ui.Button("Удалить регулятор",()=>{current();if(list.SelectedItem is ControllerSetting selected&&list.IsEnabled){model.Groups[activeCategory,activeGroup].Remove(selected);reload();}}));
            var additional=new StackPanel();var extraFields=Enumerable.Range(0,3).Select(i=>Ui.Text(additional,menu.Get("files","n"+(i==0?137:133+i),"Дополнительное значение "+(i+1)),model.AdditionalValues[i].ToString(CultureInfo.CurrentCulture))).ToArray();
            for(int i=0;i<3;i++)extraFields[i].Name="ControllerExtra"+i;
            navigation.Children.Add(Ui.Button("Дополнительные значения",()=>{current();current=()=>{};detail.IsEnabled=true;detail.Content=additional;}));reload();
            Action<string> save=target=>{current();Ui.Commit(dock);model.Comment=comment.Text;for(int i=0;i<3;i++)model.AdditionalValues[i]=InputFiles3132.Parse(extraFields[i].Text,CultureInfo.CurrentCulture);model.Save(target);path=target;list.Items.Refresh();};
            return Ui.Editor(dock,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
