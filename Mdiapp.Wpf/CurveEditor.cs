using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public sealed class CurvePlot : FrameworkElement
    {
        private Point[] points = new Point[0];
        public event Action PointsChanged;
        public Point[] Points { get => points; set { points=value; screen=new Point[0]; PointsChanged?.Invoke(); } }
        public bool ShowPrimary { get; set; } = true;
        public bool ShowSecondary { get; set; } = true;
        public Point[] SecondaryPoints { get; set; } = new Point[0];
        public bool ShowMarkers { get; set; } = true;
        public string XCaption { get; set; } = "X";
        public string YCaption { get; set; } = "Y";
        private Point[] screen = new Point[0];
        public double Zoom { get; private set; } = 1;
        private double panX,panY,rangeX=1,rangeY=1;
        private Point? drag;
        public void ResetView(){Zoom=1;panX=panY=0;InvalidateVisual();}
        public void ChangeZoom(double factor){Zoom=Math.Max(1,Math.Min(100,Zoom*factor));if(Zoom==1)panX=panY=0;InvalidateVisual();}
        public CurvePlot() { MinHeight=240;MouseWheel+=(s,e)=>{ChangeZoom(e.Delta>0?1.25:.8);e.Handled=true;};MouseLeftButtonDown+=(s,e)=>{if(e.ClickCount==2){ResetView();return;}drag=e.GetPosition(this);CaptureMouse();};MouseLeftButtonUp+=(s,e)=>{drag=null;ReleaseMouseCapture();};MouseMove+=(s,e)=>{Point cursor=e.GetPosition(this);if(drag.HasValue&&IsMouseCaptured){panX-=(cursor.X-drag.Value.X)/Math.Max(1,ActualWidth-100)*rangeX;panY+=(cursor.Y-drag.Value.Y)/Math.Max(1,ActualHeight-85)*rangeY;drag=cursor;InvalidateVisual();}if(screen.Length==0)return;int index=Enumerable.Range(0,screen.Length).OrderBy(i=>(screen[i]-cursor).LengthSquared).First();ToolTip="X: "+Points[index].X+"; Y: "+Points[index].Y;}; }
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);dc.DrawRectangle(Brushes.White,null,new Rect(RenderSize));double width=ActualWidth-100,height=ActualHeight-85;if(width<=0||height<=0)return;
            var origin=new Point(70,20+height);var pen=new Pen(Brushes.Gray,1);dc.DrawLine(pen,new Point(70,20),origin);dc.DrawLine(pen,origin,new Point(70+width,origin.Y));
            Action<string,Point> label=(text,p)=>dc.DrawText(new FormattedText(text,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),12,Brushes.Black,1),p);
            label(YCaption,new Point(8,2));label(XCaption,new Point(70,ActualHeight-25));
            screen=new Point[0];if(Points.Length==0){label("Нет точек",new Point(80,30));return;}
            var all=Points.Concat(SecondaryPoints).ToArray();double minX=all.Min(p=>p.X),maxX=all.Max(p=>p.X),minY=all.Min(p=>p.Y),maxY=all.Max(p=>p.Y);
            double dx=maxX-minX,dy=maxY-minY;if(double.IsInfinity(dx)||double.IsInfinity(dy))return;
            if(dx==0)dx=1;if(dy==0)dy=1;
            minX+=(dx-dx/Zoom)/2+panX;minY+=(dy-dy/Zoom)/2+panY;dx/=Zoom;dy/=Zoom;rangeX=dx;rangeY=dy;maxX=minX+dx;maxY=minY+dy;
            screen=Points.Select(p=>new Point(70+(p.X-minX)/dx*width,20+height-(p.Y-minY)/dy*height)).ToArray();
            label(minX.ToString("G5"),new Point(70,origin.Y+5));label(maxX.ToString("G5"),new Point(70+width-40,origin.Y+5));label(minY.ToString("G5"),new Point(3,origin.Y-15));label(maxY.ToString("G5"),new Point(3,20));
            dc.PushClip(new RectangleGeometry(new Rect(70,20,width,height)));var line=new Pen(Brushes.RoyalBlue,1.5);if(ShowPrimary){for(int i=1;i<screen.Length;i++)dc.DrawLine(line,screen[i-1],screen[i]);if(ShowMarkers)foreach(Point p in screen)dc.DrawEllipse(Brushes.RoyalBlue,null,p,3,3);}
            var second=SecondaryPoints.Select(p=>new Point(70+(p.X-minX)/dx*width,20+height-(p.Y-minY)/dy*height)).ToArray();
            var red=new Pen(Brushes.Firebrick,1.5);if(ShowSecondary){for(int i=1;i<second.Length;i++)dc.DrawLine(red,second[i-1],second[i]);if(ShowMarkers)foreach(Point p in second)dc.DrawEllipse(Brushes.Firebrick,null,p,3,3);}
            dc.Pop();
        }
    }
    public static class CurveEditor
    {
        public static UIElement Create(ApplicationSettings settings,string path,bool unit18=false)
        {
            var model=InputCurveFile.Load(path);var catalog=new Unit3Model(settings.DataDirectory,settings.Language,settings.Engine);var menu=settings.Menu;
            if(unit18 ? !new[]{5,8,18,26}.Contains(model.Type) : !new[]{1,52}.Contains(model.Type))throw new InvalidOperationException(unit18?"Для Unit18 выберите файл типа 5, 8, 18 или 26.":"Для Unit17 выберите файл типа 1 или 52.");
            var panel=CreateContent(settings,model,unit18?"n45":"f52l1",unit18?"n46":"f52l2",unit18?"n47":"f52l3",out Action commit);
            Action<string> save=target=>{commit();model.Save(target);path=target;};
            return Ui.Editor(panel,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
        internal static UIElement CreateContent(ApplicationSettings settings,InputCurveFile model,string countKey,string parameterKey,string yKey,out Action commit,bool showComment=true,int maxPoints=100,double plotHeight=300,bool showParameter=true)
        {
            var catalog=new Unit3Model(settings.DataDirectory,settings.Language,settings.Engine);var menu=settings.Menu;
            var panel=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);
            var comment=showComment?Ui.Text(top,"Комментарий",model.Comment):null;
            var choices=catalog.Parameters.Where(p=>!string.IsNullOrEmpty(p.Symbol)&&settings.IsParameterActive(p.Number)).Select(p=>new Editors.ParameterOption{Number=p.Number,Name=p.Symbol+" "+p.Name}).ToList();choices.Insert(0,new Editors.ParameterOption{Number=0,Name="Без параметра"});
            if(!choices.Any(p=>p.Number==model.Parameter))choices.Add(new Editors.ParameterOption{Number=model.Parameter,Name="Из файла (нет в каталоге)"});
            var selector=Ui.Choices(top,menu.Get("files",parameterKey,"Параметр X"),choices);selector.SelectedItem=choices.Single(p=>p.Number==model.Parameter);
            if(!showParameter){selector.Visibility=Visibility.Collapsed;top.Children[top.Children.IndexOf(selector)-1].Visibility=Visibility.Collapsed;}
            var count=Ui.Text(top,menu.Get("files",countKey,"Число точек (1–100)"),model.Count.ToString());count.Name="CurvePointCount";
            var table=new DataTable();table.Columns.Add("Row",typeof(string));for(int i=0;i<100;i++)table.Columns.Add("Point"+i,typeof(string));
            string xLabel=string.IsNullOrEmpty(model.XCaption)?selector.SelectedItem.ToString():model.XCaption;string yLabel=string.IsNullOrEmpty(model.YCaption)?menu.Get("files",yKey,"Y"):model.YCaption;
            for(int row=0;row<2;row++){var record=table.NewRow();record[0]=row==0?xLabel:yLabel;for(int col=0;col<100;col++)record[col+1]=col<model.Count?model.Values[row,col].ToString(CultureInfo.CurrentCulture):"";table.Rows.Add(record);}
            var plot=new CurvePlot{Height=plotHeight,MinHeight=Math.Min(240,plotHeight),XCaption=xLabel,YCaption=yLabel};DockPanel.SetDock(plot,Dock.Bottom);panel.Children.Add(plot);
            var grid=new DataGrid{ItemsSource=table.DefaultView,AutoGenerateColumns=false,FrozenColumnCount=1,SelectionUnit=DataGridSelectionUnit.CellOrRowHeader};panel.Children.Add(grid);
            int points=model.Count;
            Action redraw=()=>{plot.XCaption=Convert.ToString(table.Rows[0][0]);plot.Points=Enumerable.Range(0,points).Where(i=>!string.IsNullOrWhiteSpace((string)table.Rows[0][i+1])&&!string.IsNullOrWhiteSpace((string)table.Rows[1][i+1])).Select(i=>new Point(InputFiles3132.Parse((string)table.Rows[0][i+1],CultureInfo.CurrentCulture),InputFiles3132.Parse((string)table.Rows[1][i+1],CultureInfo.CurrentCulture))).Where(p=>!double.IsNaN(p.X)&&!double.IsInfinity(p.X)&&!double.IsNaN(p.Y)&&!double.IsInfinity(p.Y)).ToArray();plot.InvalidateVisual();};
            Action resize=()=>{Ui.Commit(grid);if(!int.TryParse(count.Text,out int n)||n<1||n>maxPoints)throw new InvalidOperationException("Число точек: 1–"+maxPoints+".");points=n;grid.Columns.Clear();grid.Columns.Add(new DataGridTextColumn{Header="Ряд",Binding=new Binding("Row"),IsReadOnly=true,CanUserSort=false});for(int i=0;i<n;i++)grid.Columns.Add(new DataGridTextColumn{Header=i+1,Binding=new Binding("Point"+i),CanUserSort=false});redraw();};
            top.Children.Add(Ui.Button("Применить число точек",resize));resize();table.ColumnChanged+=(s,e)=>redraw();selector.SelectionChanged+=(s,e)=>{if(selector.SelectedItem!=null)table.Rows[0][0]=selector.SelectedItem.ToString();};
            Action copy=()=>{if(grid.CurrentCell.Item is DataRowView row&&grid.CurrentColumn.DisplayIndex>0)Clipboard.SetText(Convert.ToString(row[grid.CurrentColumn.DisplayIndex]));};
            Action clear=()=>{Ui.Commit(grid);foreach(var cell in grid.SelectedCells)if(cell.Item is DataRowView row&&cell.Column.DisplayIndex>0)row[cell.Column.DisplayIndex]="";};
            Action paste=()=>{Ui.Commit(grid);if(!(grid.CurrentCell.Item is DataRowView first)||grid.CurrentColumn.DisplayIndex<1)return;int row=table.Rows.IndexOf(first.Row),column=grid.CurrentColumn.DisplayIndex;var values=InputCurveFile.ParseClipboardRows(Clipboard.GetText());int needed=column-1+values.Max(v=>v.Length);if(needed>maxPoints)throw new InvalidOperationException("Вставка превышает "+maxPoints+" точек.");if(needed>points){count.Text=needed.ToString();resize();}for(int r=0;r<values.Length&&row+r<2;r++)for(int c=0;c<values[r].Length;c++)table.Rows[row+r][column+c]=values[r][c].ToString(CultureInfo.CurrentCulture);redraw();};
            var context=new ContextMenu();foreach(var action in new[]{Tuple.Create("Копировать",copy),Tuple.Create("Вставить из Excel",paste),Tuple.Create("Очистить",clear)}){var item=new MenuItem{Header=action.Item1};item.Click+=(s,e)=>Ui.Run(action.Item2);context.Items.Add(item);}grid.ContextMenu=context;
            grid.PreviewKeyDown+=(s,e)=>{if((Keyboard.Modifiers&ModifierKeys.Control)!=0&&(e.Key==Key.V||e.Key==Key.C)){Ui.Run(e.Key==Key.V?paste:copy);e.Handled=true;}else if(e.Key==Key.Delete){Ui.Run(clear);e.Handled=true;}};
            commit=()=>{resize();if(comment!=null)model.Comment=comment.Text;model.Count=points;model.Parameter=((Editors.ParameterOption)selector.SelectedItem).Number;model.XCaption=Convert.ToString(table.Rows[0][0]);model.YCaption=yLabel;for(int row=0;row<2;row++)for(int col=0;col<points;col++)model.Values[row,col]=InputFiles3132.Parse((string)table.Rows[row][col+1],CultureInfo.CurrentCulture);};
            return panel;
        }
    }
}
