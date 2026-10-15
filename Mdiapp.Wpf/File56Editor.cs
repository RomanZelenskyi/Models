using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class File56Editor
    {
        public static UIElement Create(ApplicationSettings settings,string path)
        {
            var model=InputFile56.Load(path);var panel=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);var comment=Ui.Text(top,"Комментарий",model.Comment);var tabs=new TabControl();panel.Children.Add(tabs);var commits=new Action[2];
            Action<Panel,int> picture=(parent,number)=>{string file=Path.Combine(settings.DataDirectory,settings.Engine,settings.Language,"56_img"+number+".bmp");if(!File.Exists(file))return;var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.UriSource=new Uri(file,UriKind.Absolute);bitmap.EndInit();bitmap.Freeze();parent.Children.Add(new Image{Source=bitmap,Height=40,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(8,4,8,4)});};
            for(int group=0;group<2;group++)
            {
                int g=group;var content=new StackPanel{Margin=new Thickness(8)};string title=settings.Menu.Get("files","f56l"+(group+1),"Группа "+(group+1));if(string.IsNullOrWhiteSpace(title))title="Группа "+(group+1);Ui.Tab(tabs,title,new ScrollViewer{Content=content});
                var formulas=new WrapPanel();content.Children.Add(formulas);for(int mode=1;mode<=2;mode++){var option=new StackPanel{Width=280};formulas.Children.Add(option);option.Children.Add(new TextBlock{Text="Режим "+mode,Margin=new Thickness(8)});picture(option,mode);}
                var selector=Ui.Choices(content,"Режим",new[]{"1","2"});selector.Name="Mode39_"+g;selector.SelectedIndex=model.Modes[g]-1;var holder=new ContentControl();content.Children.Add(holder);Action commitMode=()=>{};
                Action show=()=>
                {
                    int mode=selector.SelectedIndex;var fields=new TextBox[4];var row=new WrapPanel();holder.Content=row;for(int i=0;i<4;i++){var cell=new StackPanel{Width=240,Margin=new Thickness(4)};row.Children.Add(cell);picture(cell,(mode==0?3:7)+i);fields[i]=Ui.Text(cell,"Параметр "+(i+1),model.Coefficients[g,mode,i].ToString(CultureInfo.CurrentCulture));fields[i].Name="Coefficient39_"+g+"_"+i;}
                    commitMode=()=>{for(int i=0;i<4;i++)model.Coefficients[g,mode,i]=InputFiles3132.Parse(fields[i].Text,CultureInfo.CurrentCulture);};
                };
                show();selector.SelectionChanged+=(s,e)=>{commitMode();model.Modes[g]=selector.SelectedIndex+1;show();};
                content.Children.Add(new TextBlock{Text="Дополнительные параметры",FontWeight=FontWeights.Bold,Margin=new Thickness(8,16,8,4)});var additional=new WrapPanel();content.Children.Add(additional);var extras=new TextBox[4];int[] imageNumbers={11,8,9,10};
                for(int i=0;i<4;i++){var cell=new StackPanel{Width=240,Margin=new Thickness(4)};additional.Children.Add(cell);picture(cell,imageNumbers[i]);extras[i]=Ui.Text(cell,"Параметр "+(i+1),model.AdditionalValues[g,i].ToString(CultureInfo.CurrentCulture));extras[i].Name="Extra39_"+g+"_"+i;}
                commits[g]=()=>{commitMode();model.Modes[g]=selector.SelectedIndex+1;for(int i=0;i<4;i++)model.AdditionalValues[g,i]=InputFiles3132.Parse(extras[i].Text,CultureInfo.CurrentCulture);};
            }
            Action<string> save=target=>{foreach(var commit in commits)commit();model.Comment=comment.Text;model.Save(target);path=target;};return Ui.Editor(panel,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
