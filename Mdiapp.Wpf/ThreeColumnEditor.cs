using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class ThreeColumnEditor
    {
        public static UIElement Create(ApplicationSettings settings,string path,bool unit23=false)
        {
            var model=InputThreeColumnFile.Load(path);var menu=settings.Menu;var panel=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);
            if(model.IsUnit23!=unit23)throw new InvalidOperationException(unit23?"Для Unit23 выберите тип 43, 46 или 49.":"Для Unit22 выберите тип 42, 44, 45, 47, 48 или 50.");
            var comment=Ui.Text(top,"Комментарий",model.Comment);var initial=new TextBox[unit23?3:1];
            for(int i=0;i<initial.Length;i++){initial[i]=Ui.Text(top,menu.Get("files",unit23?"n"+(i==0?79:80+i):"n74","Исходное значение "+(i+1)),model.InitialValues[i].ToString(CultureInfo.CurrentCulture));initial[i].Name=unit23?"InitialValue23_"+i:"InitialValue22";}
            var count=Ui.Text(top,menu.Get("files",unit23?"n80":"n75","Количество строк (1–1000)"),Math.Max(1,model.Rows.Count).ToString());count.Name=unit23?"RowCount23":"RowCount22";
            var markers=Ui.Check(top,menu.Get("constants","n47","Показывать точки"),false);
            string[] captions=Enumerable.Range(unit23?83:76,3).Select((n,i)=>menu.Get("files","n"+n,i==0?"X":"Y"+i)).ToArray();
            top.Children.Add(new TextBlock{Text="Синяя кривая: "+captions[1]+"; красная кривая: "+captions[2],Margin=new Thickness(8)});
            var plot=new CurvePlot{Height=280,XCaption=captions[0],YCaption=captions[1]+", "+captions[2],ShowMarkers=false};DockPanel.SetDock(plot,Dock.Bottom);panel.Children.Add(plot);
            var table=new DataTable();table.Columns.Add("Index",typeof(int));for(int i=0;i<3;i++)table.Columns.Add("Value"+i,typeof(string));
            for(int row=0;row<1000;row++)table.Rows.Add(new object[]{row+1}.Concat(Enumerable.Range(0,3).Select(col=>(object)(row<model.Rows.Count?model.Rows[row][col].ToString(CultureInfo.CurrentCulture):""))).ToArray());
            var grid=new DataGrid{ItemsSource=table.DefaultView,AutoGenerateColumns=false,FrozenColumnCount=1,SelectionUnit=DataGridSelectionUnit.CellOrRowHeader};panel.Children.Add(grid);
            grid.Columns.Add(new DataGridTextColumn{Header="№",Binding=new Binding("Index"),IsReadOnly=true,CanUserSort=false});for(int i=0;i<3;i++)grid.Columns.Add(new DataGridTextColumn{Header=captions[i],Binding=new Binding("Value"+i),CanUserSort=false,Width=new DataGridLength(1,DataGridLengthUnitType.Star)});
            int rows=Math.Max(1,model.Rows.Count);
            Action redraw=()=>
            {
                var values=table.Rows.Cast<DataRow>().Take(rows).Where(row=>Enumerable.Range(1,3).All(i=>!string.IsNullOrWhiteSpace((string)row[i]))).Select(row=>Enumerable.Range(1,3).Select(i=>InputFiles3132.Parse((string)row[i],CultureInfo.CurrentCulture)).ToArray()).Where(row=>row.All(v=>!double.IsNaN(v)&&!double.IsInfinity(v))).ToArray();
                plot.Points=values.Select(row=>new Point(row[0],row[1])).ToArray();plot.SecondaryPoints=values.Select(row=>new Point(row[0],row[2])).ToArray();plot.ShowMarkers=markers.IsChecked==true;plot.InvalidateVisual();
            };
            Action resize=()=>{Ui.Commit(grid);if(!int.TryParse(count.Text,out int n)||n<1||n>1000)throw new InvalidOperationException("Количество строк: 1–1000.");rows=n;table.DefaultView.RowFilter="Index <= "+n;redraw();};
            top.Children.Add(Ui.Button("Применить количество строк",resize));resize();table.ColumnChanged+=(s,e)=>redraw();markers.Checked+=(s,e)=>redraw();markers.Unchecked+=(s,e)=>redraw();
            Action copy=()=>{if(grid.CurrentCell.Item is DataRowView row&&grid.CurrentColumn.DisplayIndex>0)Clipboard.SetText(Convert.ToString(row[grid.CurrentColumn.DisplayIndex]));};
            Action clear=()=>{Ui.Commit(grid);foreach(var cell in grid.SelectedCells)if(cell.Item is DataRowView row&&cell.Column.DisplayIndex>0)row[cell.Column.DisplayIndex]="";};
            Action paste=()=>{Ui.Commit(grid);if(!(grid.CurrentCell.Item is DataRowView first)||grid.CurrentColumn.DisplayIndex<1)return;int start=table.Rows.IndexOf(first.Row),column=grid.CurrentColumn.DisplayIndex;var values=InputThreeColumnFile.ParseClipboardRows(Clipboard.GetText());int needed=start+values.Length;if(needed>1000||values.Any(row=>column-1+row.Length>3))throw new InvalidOperationException("Вставка выходит за пределы 1000 строк или трёх столбцов.");if(needed>rows){count.Text=needed.ToString();resize();}for(int r=0;r<values.Length;r++)for(int c=0;c<values[r].Length;c++)table.Rows[start+r][column+c]=values[r][c].ToString(CultureInfo.CurrentCulture);redraw();};
            var context=new ContextMenu();foreach(var action in new[]{Tuple.Create("Копировать",copy),Tuple.Create("Вставить из Excel",paste),Tuple.Create("Очистить",clear)}){var item=new MenuItem{Header=action.Item1};item.Click+=(s,e)=>Ui.Run(action.Item2);context.Items.Add(item);}grid.ContextMenu=context;
            grid.PreviewKeyDown+=(s,e)=>{if((Keyboard.Modifiers&ModifierKeys.Control)!=0&&(e.Key==Key.V||e.Key==Key.C)){Ui.Run(e.Key==Key.V?paste:copy);e.Handled=true;}else if(e.Key==Key.Delete){Ui.Run(clear);e.Handled=true;}};
            Action<string> save=target=>{resize();model.Comment=comment.Text;for(int i=0;i<initial.Length;i++)model.InitialValues[i]=InputFiles3132.Parse(initial[i].Text,CultureInfo.CurrentCulture);model.Rows.Clear();for(int row=0;row<rows;row++)model.Rows.Add(Enumerable.Range(1,3).Select(i=>InputFiles3132.Parse((string)table.Rows[row][i],CultureInfo.CurrentCulture)).ToArray());model.Save(target);path=target;};
            return Ui.Editor(panel,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
