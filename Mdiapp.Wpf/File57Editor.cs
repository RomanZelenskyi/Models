using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class File57Editor
    {
        public static UIElement Create(ApplicationSettings settings,string path)
        {
            var model=InputFile57.Load(path);var panel=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);var comment=Ui.Text(top,"Комментарий",model.Comment);
            Action<Panel,int> picture=(parent,number)=>{string file=Path.Combine(settings.DataDirectory,settings.Engine,settings.Language,"57_img"+number+".bmp");if(!File.Exists(file))return;var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.UriSource=new Uri(file,UriKind.Absolute);bitmap.EndInit();bitmap.Freeze();parent.Children.Add(new Image{Source=bitmap,Height=35,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(8,4,8,4)});};
            var constants=new WrapPanel();top.Children.Add(constants);var fields=new TextBox[3];for(int i=0;i<3;i++){var cell=new StackPanel{Width=300,Margin=new Thickness(4)};constants.Children.Add(cell);picture(cell,i+1);fields[i]=Ui.Text(cell,"Исходное значение "+(i+1),model.Constants[i].ToString(CultureInfo.CurrentCulture));fields[i].Name="Constant40_"+i;}
            var tabs=new TabControl();panel.Children.Add(tabs);var commits=new Action[2];for(int i=0;i<2;i++){var content=new DockPanel();var images=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(images,Dock.Top);content.Children.Add(images);picture(images,i+4);content.Children.Add(CurveEditor.CreateContent(settings,model.Curves[i],"f40l"+(1+i*2),"f40l"+(2+i*2),"f40t"+(i+1),out commits[i],false,100,180));Ui.Tab(tabs,"Характеристика "+(i+1),content);}
            Action<string> save=target=>{Ui.Commit(panel);foreach(var commit in commits)commit();for(int i=0;i<3;i++){if(!double.TryParse(fields[i].Text,NumberStyles.Float,CultureInfo.CurrentCulture,out double value)&&!double.TryParse(fields[i].Text,NumberStyles.Float,CultureInfo.InvariantCulture,out value))value=1;model.Constants[i]=value==-1000?1:value;}model.Comment=comment.Text;model.Save(target);path=target;};return Ui.Editor(panel,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
