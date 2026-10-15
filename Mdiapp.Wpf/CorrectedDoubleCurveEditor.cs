using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class CorrectedDoubleCurveEditor
    {
        private static System.Collections.Generic.IEnumerable<DependencyObject> Children(DependencyObject root){yield return root;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Children(VisualTreeHelper.GetChild(root,i)))yield return child;}
        public static UIElement Create(ApplicationSettings settings,string path)
        {
            var model=CorrectedDoubleCurveFile.Load(path);var dock=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);dock.Children.Add(top);var comment=Ui.Text(top,"Комментарий",model.Data.Comment);
            var referencesPanel=new StackPanel{Orientation=Orientation.Horizontal};top.Children.Add(referencesPanel);var references=Enumerable.Range(0,4).Select(i=>Ui.Text(referencesPanel,"r"+(i+1),model.References[i].ToString(CultureInfo.CurrentCulture))).ToArray();for(int i=0;i<4;i++)references[i].Name="Reference28_"+i;
            top.Children.Add(new TextBlock{Text="Yиспр = Y · [1 + a·(X−r1)/r2 + b·(X−r3)/r4 + c]. Нулевые r2/r4 считаются равными 1.",Margin=new Thickness(8)});
            var switches=new StackPanel{Orientation=Orientation.Horizontal};top.Children.Add(switches);var original=Ui.Check(switches,"Исходные кривые",true);var corrected=Ui.Check(switches,"Исправленные кривые",true);var bake=Ui.Check(switches,"Сохранить с применённой коррекцией",false);bake.Name="BakeCorrection28";
            var tabs=new TabControl();dock.Children.Add(tabs);var commits=new Action[2];var plots=new CurvePlot[2];var fields=new TextBox[2,3];
            Action read=()=>{for(int i=0;i<4;i++)model.References[i]=InputFiles3132.Parse(references[i].Text,CultureInfo.CurrentCulture);for(int curve=0;curve<2;curve++)for(int i=0;i<3;i++)if(fields[curve,i]!=null)model.Coefficients[curve,i]=InputFiles3132.Parse(fields[curve,i].Text,CultureInfo.CurrentCulture);};
            Action redraw=()=>{read();for(int i=0;i<2;i++){var plot=plots[i];if(plot==null)continue;plot.ShowPrimary=original.IsChecked==true;plot.ShowSecondary=corrected.IsChecked==true;try{plot.SecondaryPoints=plot.Points.Select(p=>new Point(p.X,model.Correct(i,p.X,p.Y))).ToArray();}catch(System.IO.InvalidDataException){plot.SecondaryPoints=new Point[0];}plot.InvalidateVisual();}};
            for(int i=0;i<2;i++)
            {
                int curveIndex=i;var panel=new DockPanel();var bar=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(bar,Dock.Top);panel.Children.Add(bar);
                for(int k=0;k<3;k++){fields[i,k]=Ui.Text(bar,new[]{"a","b","c"}[k],settings.CorrectionPreset(14)[i*3+k].ToString(CultureInfo.CurrentCulture));fields[i,k].Name="Correction28_"+i+"_"+k;fields[i,k].TextChanged+=(s,e)=>redraw();}
                var curve=CurveEditor.CreateContent(settings,model.Data.Curves[i],"n"+(51+i*3),"n"+(52+i*3),"n"+(53+i*3),out commits[i],false,100,160);panel.Children.Add(curve);Ui.Tab(tabs,"Характеристика "+(i+1),panel);
                ((FrameworkElement)curve).Loaded+=(s,e)=>{if(plots[curveIndex]!=null)return;plots[curveIndex]=Children(curve).OfType<CurvePlot>().Single();plots[curveIndex].PointsChanged+=redraw;redraw();};
            }
            var constants=new StackPanel();Ui.Tab(tabs,"Дополнительные значения",constants);var constantFields=Enumerable.Range(0,3).Select(i=>Ui.Text(constants,settings.Menu.Get("files","n"+(57+i),"Значение "+(i+1)),model.Data.Constants[i].ToString(CultureInfo.CurrentCulture))).ToArray();
            foreach(var field in references)field.TextChanged+=(s,e)=>redraw();original.Checked+=(s,e)=>redraw();original.Unchecked+=(s,e)=>redraw();corrected.Checked+=(s,e)=>redraw();corrected.Unchecked+=(s,e)=>redraw();
            Action<string> save=target=>{Ui.Commit(dock);foreach(var commit in commits)commit();read();model.Data.Comment=comment.Text;for(int i=0;i<3;i++)model.Data.Constants[i]=InputFiles3132.Parse(constantFields[i].Text,CultureInfo.CurrentCulture);model.Save(target,bake.IsChecked==true,CultureInfo.CurrentCulture,settings.Menu.Get("constants","n52","Коррекция применена"));path=target;};
            return Ui.Editor(dock,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
