using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public sealed class CurveFamilyPlot : FrameworkElement
    {
        public Point[][] Series { get; set; } = new Point[0][];
        public string[] Labels { get; set; } = new string[0];
        public string XCaption { get; set; } = "X";
        public string YCaption { get; set; } = "Y";
        public CurveFamilyPlot(){MinHeight=250;}
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);dc.DrawRectangle(Brushes.White,null,new Rect(RenderSize));var points=Series.SelectMany(s=>s).Where(p=>!double.IsNaN(p.X)&&!double.IsInfinity(p.X)&&!double.IsNaN(p.Y)&&!double.IsInfinity(p.Y)).ToArray();double w=ActualWidth-100,h=ActualHeight-105;if(points.Length==0||w<=0||h<=0)return;
            double minX=points.Min(p=>p.X),maxX=points.Max(p=>p.X),minY=points.Min(p=>p.Y),maxY=points.Max(p=>p.Y),dx=maxX-minX,dy=maxY-minY;if(double.IsInfinity(dx)||double.IsInfinity(dy))return;if(dx==0)dx=1;if(dy==0)dy=1;
            Action<string,Point,Brush> text=(label,p,brush)=>dc.DrawText(new FormattedText(label,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),12,brush,1),p);
            var gray=new Pen(Brushes.Gray,1);dc.DrawLine(gray,new Point(70,25),new Point(70,25+h));dc.DrawLine(gray,new Point(70,25+h),new Point(70+w,25+h));
            text(YCaption,new Point(5,2),Brushes.Black);text(XCaption,new Point(70,ActualHeight-48),Brushes.Black);text(minX.ToString("G5"),new Point(70,30+h),Brushes.Black);text(maxX.ToString("G5"),new Point(70+w-50,30+h),Brushes.Black);text(minY.ToString("G5"),new Point(3,15+h),Brushes.Black);text(maxY.ToString("G5"),new Point(3,25),Brushes.Black);
            Brush[] palette={Brushes.RoyalBlue,Brushes.Firebrick,Brushes.ForestGreen,Brushes.DarkOrange,Brushes.Purple,Brushes.Teal,Brushes.DeepPink,Brushes.Sienna};
            for(int i=0;i<Series.Length;i++)
            {
                var brush=palette[i%palette.Length];var pen=new Pen(brush,1.5);var values=Series[i].Where(p=>!double.IsNaN(p.X)&&!double.IsInfinity(p.X)&&!double.IsNaN(p.Y)&&!double.IsInfinity(p.Y)).Select(p=>new Point(70+(p.X-minX)/dx*w,25+h-(p.Y-minY)/dy*h)).ToArray();for(int j=1;j<values.Length;j++)dc.DrawLine(pen,values[j-1],values[j]);foreach(var point in values)dc.DrawEllipse(brush,null,point,2,2);
            }
        }
    }
    public static class MultiCharacteristicEditor
    {
        public static UIElement Create(ApplicationSettings settings,string path)
        {
            var model=MultiCharacteristicFile.Load(path);var menu=settings.Menu;var dock=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);dock.Children.Add(top);var comment=Ui.Text(top,"Комментарий",model.Comment);
            var navigation=new StackPanel{Width=230};DockPanel.SetDock(navigation,Dock.Left);dock.Children.Add(navigation);var detail=new ContentControl();dock.Children.Add(detail);
            var characteristics=Ui.Choices(navigation,menu.Get("files","f1500l2","Характеристика"),model.Characteristics);characteristics.Name="Characteristics26";
            var branches=new ListBox{Height=250,Margin=new Thickness(8),Name="Branches26"};navigation.Children.Add(branches);Action current=()=>{};bool updating=false;MultiCharacteristic active=null;
            Action<MultiCharacteristicBranch> render=branch=>
            {
                var body=new DockPanel();var header=new StackPanel();DockPanel.SetDock(header,Dock.Top);body.Children.Add(header);var mach=Ui.Text(header,menu.Get("files","f1500l3","Число Маха"),active.Mach.ToString(CultureInfo.CurrentCulture));mach.Name="Mach26";
                var value=Ui.Text(header,menu.Get("files","f1500l6","Значение ветви"),branch.Value.ToString(CultureInfo.CurrentCulture));value.Name="BranchValue26";
                var tabs=new TabControl();body.Children.Add(tabs);
                if(string.IsNullOrEmpty(branch.Curve.XCaption))branch.Curve.XCaption=menu.Get("files","f1500l4","X");if(string.IsNullOrEmpty(branch.Curve.YCaption))branch.Curve.YCaption=menu.Get("files","f1500l5","Y");
                var curve=CurveEditor.CreateContent(settings,branch.Curve,"n26","f1500l6","f1500l5",out Action commit,false,100,180,false);Ui.Tab(tabs,"Выбранная ветвь",curve);
                var familyPanel=new DockPanel();var legend=new WrapPanel();DockPanel.SetDock(legend,Dock.Bottom);familyPanel.Children.Add(legend);var family=new CurveFamilyPlot{XCaption=branch.Curve.XCaption,YCaption=branch.Curve.YCaption};familyPanel.Children.Add(family);Ui.Tab(tabs,"Все ветви",familyPanel);
                current=()=>{Ui.Commit(body);active.Mach=InputFiles3132.Parse(mach.Text,CultureInfo.CurrentCulture);branch.Value=InputFiles3132.Parse(value.Text,CultureInfo.CurrentCulture);commit();};
                tabs.SelectionChanged+=(s,e)=>{if(e.Source!=tabs||tabs.SelectedIndex!=1)return;Ui.Run(()=>{current();family.Series=active.Branches.Select(b=>Enumerable.Range(0,b.Curve.Count).Select(i=>new Point(b.Curve.Values[0,i],b.Curve.Values[1,i])).ToArray()).ToArray();family.Labels=active.Branches.Select(b=>b.Value.ToString(CultureInfo.CurrentCulture)).ToArray();legend.Children.Clear();Brush[] colors={Brushes.RoyalBlue,Brushes.Firebrick,Brushes.ForestGreen,Brushes.DarkOrange,Brushes.Purple,Brushes.Teal,Brushes.DeepPink,Brushes.Sienna};for(int i=0;i<family.Labels.Length;i++)legend.Children.Add(new TextBlock{Text=(i+1)+": "+family.Labels[i],Foreground=colors[i%colors.Length],Margin=new Thickness(8)});family.InvalidateVisual();});};detail.Content=body;
            };
            Action reload=()=>{current();current=()=>{};updating=true;active=characteristics.SelectedItem as MultiCharacteristic;branches.ItemsSource=null;branches.ItemsSource=active?.Branches;branches.SelectedIndex=branches.Items.Count>0?0:-1;updating=false;detail.Content=null;if(branches.SelectedItem is MultiCharacteristicBranch branch)render(branch);};
            characteristics.SelectionChanged+=(s,e)=>{if(!updating&&e.Source==characteristics)Ui.Run(reload);};branches.SelectionChanged+=(s,e)=>{if(updating)return;Ui.Run(()=>{current();current=()=>{};detail.Content=null;if(branches.SelectedItem is MultiCharacteristicBranch branch)render(branch);});};
            navigation.Children.Add(Ui.Button("Добавить характеристику",()=>{current();if(model.Characteristics.Count>=30)throw new InvalidOperationException("Характеристик не более 30.");var item=new MultiCharacteristic();item.Branches.Add(new MultiCharacteristicBranch());model.Characteristics.Add(item);updating=true;characteristics.Items.Refresh();characteristics.SelectedItem=item;updating=false;reload();}));
            navigation.Children.Add(Ui.Button("Удалить характеристику",()=>{current();if(model.Characteristics.Count<=1)throw new InvalidOperationException("Оставьте хотя бы одну характеристику.");model.Characteristics.Remove(active);updating=true;characteristics.Items.Refresh();characteristics.SelectedIndex=0;updating=false;reload();}));
            navigation.Children.Add(Ui.Button("Добавить ветвь",()=>{current();if(active.Branches.Count>=30)throw new InvalidOperationException("Ветвей не более 30.");active.Branches.Add(new MultiCharacteristicBranch());reload();branches.SelectedIndex=active.Branches.Count-1;}));
            navigation.Children.Add(Ui.Button("Удалить ветвь",()=>{current();if(active.Branches.Count<=1)throw new InvalidOperationException("Оставьте хотя бы одну ветвь.");if(branches.SelectedItem is MultiCharacteristicBranch branch)active.Branches.Remove(branch);reload();}));
            updating=true;characteristics.SelectedIndex=0;updating=false;reload();Action<string> save=target=>{current();model.Comment=comment.Text;model.Save(target);path=target;};return Ui.Editor(dock,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
