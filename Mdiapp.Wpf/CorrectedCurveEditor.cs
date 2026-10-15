using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class CorrectedCurveEditor
    {
        private static System.Collections.Generic.IEnumerable<DependencyObject> Children(DependencyObject root){yield return root;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Children(VisualTreeHelper.GetChild(root,i)))yield return child;}
        public static UIElement Create(ApplicationSettings settings,string path,bool unit29=false)
        {
            var model=CorrectedCurveFile.Load(path,simple:unit29);var dock=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);dock.Children.Add(top);
            var bar=new StackPanel{Orientation=Orientation.Horizontal};top.Children.Add(bar);var correction=new[]{"a","b","c"}.Select((name,i)=>Ui.Text(bar,name,settings.CorrectionPreset(model.Curve.Type)[i].ToString(CultureInfo.CurrentCulture))).ToArray();for(int i=0;i<3;i++)correction[i].Name="Correction"+i;
            var referencesPanel=new StackPanel{Orientation=Orientation.Horizontal};top.Children.Add(referencesPanel);var references=new[]{"r1","r2","r3","r4"}.Select((name,i)=>Ui.Text(referencesPanel,name,model.References[i].ToString(CultureInfo.CurrentCulture))).ToArray();
            if(unit29){correction[1].Visibility=Visibility.Collapsed;bar.Children[bar.Children.IndexOf(correction[1])-1].Visibility=Visibility.Collapsed;for(int i=2;i<4;i++){references[i].Visibility=Visibility.Collapsed;referencesPanel.Children[referencesPanel.Children.IndexOf(references[i])-1].Visibility=Visibility.Collapsed;}}
            top.Children.Add(new TextBlock{Text=unit29?"Yиспр = Y · [1 + a·(X−r1)/r2 + c]. Нулевой r2 считается равным 1.":"Yиспр = Y · [1 + a·(X−r1)/r2 + b·(X−r3)/r4 + c]. Нулевые r2/r4 считаются равными 1.",Margin=new Thickness(8)});
            var switches=new StackPanel{Orientation=Orientation.Horizontal};top.Children.Add(switches);var original=Ui.Check(switches,"Исходная кривая (синяя)",true);var corrected=Ui.Check(switches,"Исправленная кривая (красная)",true);var bake=Ui.Check(switches,"Сохранить с применённой коррекцией",false);bake.Name="BakeCorrection";
            var curve=CurveEditor.CreateContent(settings,model.Curve,unit29?"n45":"n42",unit29?"n46":"n43",unit29?"n47":"n44",out Action commit,true,100,160);dock.Children.Add(curve);
            Action read=()=>{model.A=InputFiles3132.Parse(correction[0].Text,CultureInfo.CurrentCulture);model.B=InputFiles3132.Parse(correction[1].Text,CultureInfo.CurrentCulture);model.C=InputFiles3132.Parse(correction[2].Text,CultureInfo.CurrentCulture);for(int i=0;i<4;i++)model.References[i]=InputFiles3132.Parse(references[i].Text,CultureInfo.CurrentCulture);};
            CurvePlot plot=null;
            Action redraw=()=>
            {
                read();if(plot==null)return;plot.ShowPrimary=original.IsChecked==true;plot.ShowSecondary=corrected.IsChecked==true;
                try{plot.SecondaryPoints=plot.Points.Select(p=>new Point(p.X,model.Correct(p.X,p.Y))).ToArray();}catch(System.IO.InvalidDataException){plot.SecondaryPoints=new Point[0];}plot.InvalidateVisual();
            };
            ((FrameworkElement)curve).Loaded+=(s,e)=>{if(plot!=null)return;plot=Children(curve).OfType<CurvePlot>().Single();plot.PointsChanged+=redraw;redraw();};
            foreach(var field in correction.Concat(references))field.TextChanged+=(s,e)=>redraw();original.Checked+=(s,e)=>redraw();original.Unchecked+=(s,e)=>redraw();corrected.Checked+=(s,e)=>redraw();corrected.Unchecked+=(s,e)=>redraw();
            Action<string> save=target=>{Ui.Commit(dock);commit();read();model.Save(target,bake.IsChecked==true,CultureInfo.CurrentCulture,settings.Menu.Get("constants","n52","Коррекция применена"));path=target;};
            return Ui.Editor(dock,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
