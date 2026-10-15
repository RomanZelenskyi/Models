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
    public static class TransitionEditor
    {
        public static UIElement Create(ApplicationSettings settings,string path)
        {
            var model=InputTransitionFile.Load(path);var panel=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);var comment=Ui.Text(top,"Комментарий",model.Comment);var scale=Ui.Text(top,"Масштаб",model.Scale.ToString(CultureInfo.CurrentCulture));scale.Name="Scale36";
            string picture=Path.Combine(settings.DataDirectory,settings.Engine,settings.Language,model.Type+"_img1.bmp");if(File.Exists(picture)){var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.UriSource=new Uri(picture,UriKind.Absolute);bitmap.EndInit();bitmap.Freeze();top.Children.Add(new Image{Source=bitmap,Height=45,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(8,3,8,0)});}
            model.Curve.XCaption=settings.Menu.Get("files","f43n1","X");model.Curve.YCaption=settings.Menu.Get("files","f43n2","Y");
            var tabs=new TabControl();panel.Children.Add(tabs);Ui.Tab(tabs,"Основная характеристика",CurveEditor.CreateContent(settings,model.Curve,"f43l1","f43n1","f43n2",out Action curveCommit,false,100,240,false));
            var correction=model.Correction;var coefficients=new DockPanel();Ui.Tab(tabs,"Переходные коэффициенты",coefficients);var controls=new StackPanel();DockPanel.SetDock(controls,Dock.Top);coefficients.Children.Add(controls);
            var branches=Ui.Text(controls,settings.Menu.Get("files","f43l2","Число ветвей (1–30)"),correction.BranchCount.ToString());branches.Name="Branches36";var points=Ui.Text(controls,settings.Menu.Get("files","f43l3","Число точек (1–100)"),correction.Arguments.Length.ToString());points.Name="Points36";
            var holder=new ContentControl();coefficients.Children.Add(holder);Action correctionCommit=()=>{};
            Action refresh=()=>holder.Content=SurfaceEditor.Matrix(1+correction.BranchCount*2,correction.Arguments.Length,(r,c)=>r==0?correction.Arguments[c]:correction.Values[r-1,c],(r,c,v)=>{if(r==0)correction.Arguments[c]=v;else correction.Values[r-1,c]=v;},r=>r==0?settings.Menu.Get("files","f43n3","Аргумент"):settings.Menu.Get("files",r%2==1?"f43n4":"f43n5",r%2==1?"k":"T")+" "+((r+1)/2),c=>(c+1).ToString(),out correctionCommit);
            controls.Children.Add(Ui.Button("Применить размеры",()=>{int b=Number(branches,30),p=Number(points,100);correctionCommit();correction.Resize(b,p);refresh();}));refresh();
            Action<string> save=target=>{int b=Number(branches,30),p=Number(points,100);Ui.Commit(panel);curveCommit();correctionCommit();correction.Resize(b,p);model.Scale=InputFiles3132.Parse(scale.Text,CultureInfo.CurrentCulture);model.Comment=comment.Text;model.Save(target);path=target;refresh();};
            return Ui.Editor(panel,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
        private static int Number(TextBox field,int maximum){if(!int.TryParse(field.Text,out int number)||number<1||number>maximum)throw new InvalidOperationException("Количество должно быть от 1 до "+maximum+".");return number;}
    }
}
