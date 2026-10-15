using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class CorrectedTripleCurveEditor
    {
        private static System.Collections.Generic.IEnumerable<DependencyObject> Children(DependencyObject root){yield return root;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Children(VisualTreeHelper.GetChild(root,i)))yield return child;}
        public static UIElement Create(ApplicationSettings settings,string path)
        {
            var model=CorrectedTripleCurveFile.Load(path);var dock=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);dock.Children.Add(top);var comment=Ui.Text(top,"Комментарий",model.Data.Comment);
            var switches=new StackPanel{Orientation=Orientation.Horizontal};top.Children.Add(switches);var original=Ui.Check(switches,"Исходные кривые",true);var corrected=Ui.Check(switches,"Исправленные кривые",true);var bake=Ui.Check(switches,"Сохранить с применённой коррекцией",false);bake.Name="BakeCorrection33";
            var tabs=new TabControl();dock.Children.Add(tabs);var commits=new Action[3];var plots=new CurvePlot[3];var fields=new TextBox[3,3];var references=new TextBox[3][];
            Action read=()=>{for(int curve=0;curve<3;curve++){if(references[curve]!=null)for(int i=0;i<references[curve].Length;i++)model.References[curve][i]=InputFiles3132.Parse(references[curve][i].Text,CultureInfo.CurrentCulture);for(int i=0;i<3;i++)if(fields[curve,i]!=null)model.Coefficients[curve,i]=InputFiles3132.Parse(fields[curve,i].Text,CultureInfo.CurrentCulture);}};
            Action redraw=()=>{read();for(int i=0;i<3;i++){var plot=plots[i];if(plot==null)continue;plot.ShowPrimary=original.IsChecked==true;plot.ShowSecondary=corrected.IsChecked==true;try{plot.SecondaryPoints=plot.Points.Select(p=>new Point(p.X,model.Correct(i,p.X,p.Y))).ToArray();}catch(System.IO.InvalidDataException){plot.SecondaryPoints=new Point[0];}plot.InvalidateVisual();}};
            for(int i=0;i<3;i++)
            {
                int curveIndex=i;var panel=new DockPanel();var header=new StackPanel();DockPanel.SetDock(header,Dock.Top);panel.Children.Add(header);
                var refRow=new StackPanel{Orientation=Orientation.Horizontal};header.Children.Add(refRow);references[i]=Enumerable.Range(0,model.References[i].Length).Select(k=>Ui.Text(refRow,"r"+(k+1),model.References[i][k].ToString(CultureInfo.CurrentCulture))).ToArray();foreach(var field in references[i])field.TextChanged+=(s,e)=>redraw();
                var bar=new StackPanel{Orientation=Orientation.Horizontal};header.Children.Add(bar);for(int k=0;k<3;k++){if(k==1&&i<2)continue;fields[i,k]=Ui.Text(bar,new[]{"a","b","c"}[k],settings.CorrectionPreset(model.Data.Type)[i*3+k].ToString(CultureInfo.CurrentCulture));fields[i,k].Name="Correction33_"+i+"_"+k;fields[i,k].TextChanged+=(s,e)=>redraw();}
                header.Children.Add(new TextBlock{Text=i<2?"Yиспр=Y·[1+a·(X−r1)/r2+c]":"Yиспр=Y·[1+a·(X−r1)/r2+b·(X−r3)/r4+c]",Margin=new Thickness(8)});
                var curve=CurveEditor.CreateContent(settings,model.Data.Curves[i],"n"+(60+i*3),"n"+(61+i*3),"n"+(62+i*3),out commits[i],false,100,160);panel.Children.Add(curve);Ui.Tab(tabs,"Характеристика "+(i+1),panel);
                ((FrameworkElement)curve).Loaded+=(s,e)=>{if(plots[curveIndex]!=null)return;plots[curveIndex]=Children(curve).OfType<CurvePlot>().Single();plots[curveIndex].PointsChanged+=redraw;redraw();};
            }
            original.Checked+=(s,e)=>redraw();original.Unchecked+=(s,e)=>redraw();corrected.Checked+=(s,e)=>redraw();corrected.Unchecked+=(s,e)=>redraw();
            Action<string> save=target=>{Ui.Commit(dock);foreach(var commit in commits)commit();read();model.Data.Comment=comment.Text;model.Save(target,bake.IsChecked==true,CultureInfo.CurrentCulture,settings.Menu.Get("constants","n52","Коррекция применена"));path=target;};
            return Ui.Editor(dock,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
