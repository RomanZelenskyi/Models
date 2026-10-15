using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class CharacteristicEditor
    {
        public static UIElement Create(ApplicationSettings settings,string path)
        {
            var model=CharacteristicFile.Load(path);var content=CreateContent(settings,model,out Action commit);
            Action<string> save=target=>{commit();model.Save(target);path=target;};return Ui.Editor(content,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
        internal static UIElement CreateContent(ApplicationSettings settings,CharacteristicFile model,out Action commit,int maxPoints=99)
        {
            var catalog=new Unit3Model(settings.DataDirectory,settings.Language,settings.Engine);var menu=settings.Menu;
            var labels=new[]{"Ряд 1","Ряд 2","Ряд 3"};var graph=catalog.Graphs.FirstOrDefault(g=>g.n==model.Type);
            if(graph!=null)
            {
                int[] rows={graph.StandartFile_X,graph.StandartFile_top_Y,graph.StandartFile_bottom_Y};int[] keys={graph.Key_X,graph.Key_top_Y,graph.Key_bottom_Y};
                for(int i=0;i<3;i++)if(rows[i]>=0&&rows[i]<3)labels[rows[i]]=catalog.Parameters.FirstOrDefault(p=>p.Number==keys[i])?.Symbol??labels[rows[i]];
            }
            var tabs=new TabControl();var options=new StackPanel();Ui.Tab(tabs,"Настройки",new ScrollViewer{Content=options});
            var comment=Ui.Text(options,"Комментарий",model.Comment);
            var coefficients=model.Coefficients.Select((v,i)=>Ui.Text(options,menu.Get("files","n"+(i+(model.IsSpecial?34:28)),"Коэффициент "+(i+1)),v.ToString(CultureInfo.CurrentCulture))).ToArray();
            var mode=Ui.Choices(options,"Интерполяция",new[]{"0","5","-5","10","-10"});mode.SelectedItem=model.Interpolation.ToString();mode.IsEnabled=!model.IsSpecial;
            var extra=Ui.Text(options,menu.Get("files","n33","Дополнительное значение"),model.AdditionalValue.ToString(CultureInfo.CurrentCulture));extra.IsEnabled=!model.IsSpecial;
            var data=new DockPanel();Ui.Tab(tabs,"Ветви и точки",data);
            var toolbar=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(toolbar,Dock.Top);data.Children.Add(toolbar);
            var branchCount=Ui.Text(toolbar,"Ветвей (1–30)",Math.Max(1,model.Branches.Count).ToString());branchCount.Name="BranchCount";
            var pointCount=Ui.Text(toolbar,"Точек (1–"+maxPoints+")",Math.Max(1,model.Branches.Count==0?0:model.Branches.Max(b=>b.Points.GetLength(1))).ToString());pointCount.Name="PointCount";
            var table=new DataTable();table.Columns.Add("Branch",typeof(int));table.Columns.Add("Row",typeof(string));table.Columns.Add("BranchValue",typeof(string));
            for(int i=0;i<maxPoints;i++)table.Columns.Add("Point"+i,typeof(string));
            for(int branch=0;branch<30;branch++)for(int row=0;row<3;row++)
            {
                var record=table.NewRow();record[0]=branch+1;record[1]=labels[row];record[2]=row==0&&branch<model.Branches.Count?model.Branches[branch].Value.ToString(CultureInfo.CurrentCulture):"";
                for(int col=0;col<maxPoints;col++)record[col+3]=branch<model.Branches.Count&&col<model.Branches[branch].Points.GetLength(1)?model.Branches[branch].Points[row,col].ToString(CultureInfo.CurrentCulture):"";
                table.Rows.Add(record);
            }
            var grid=new DataGrid{AutoGenerateColumns=false,ItemsSource=table.DefaultView,FrozenColumnCount=3,SelectionUnit=DataGridSelectionUnit.CellOrRowHeader};data.Children.Add(grid);
            int branches=1,points=1;
            Action resize=()=>
            {
                Ui.Commit(grid);
                if(!int.TryParse(branchCount.Text,out int b)||b<1||b>30||!int.TryParse(pointCount.Text,out int p)||p<1||p>maxPoints)throw new InvalidOperationException("Ветвей: 1–30; точек: 1–"+maxPoints+".");
                branches=b;points=p;table.DefaultView.RowFilter="Branch <= "+b;grid.Columns.Clear();
                string[] names={"Branch","Row","BranchValue"};string[] captions={"Ветвь","Ряд","Значение ветви"};
                for(int i=0;i<3;i++)grid.Columns.Add(new DataGridTextColumn{Header=captions[i],Binding=new Binding(names[i]),IsReadOnly=i<2,CanUserSort=false});
                for(int i=0;i<p;i++)grid.Columns.Add(new DataGridTextColumn{Header=i+1,Binding=new Binding("Point"+i),CanUserSort=false});
            };
            toolbar.Children.Add(Ui.Button("Применить размер",resize));resize();
            grid.BeginningEdit+=(s,e)=>{if(e.Column.DisplayIndex==2&&table.Rows.IndexOf(((DataRowView)e.Row.Item).Row)%3!=0)e.Cancel=true;};
            Action copy=()=>{if(grid.CurrentCell.Item is DataRowView row&&grid.CurrentColumn.DisplayIndex>=2)Clipboard.SetText(Convert.ToString(row[grid.CurrentColumn.DisplayIndex]));};
            Action clear=()=>{foreach(var cell in grid.SelectedCells)if(cell.Item is DataRowView row&&cell.Column.DisplayIndex>=2&&(cell.Column.DisplayIndex!=2||table.Rows.IndexOf(row.Row)%3==0))row[cell.Column.DisplayIndex]="";};
            Action paste=()=>
            {
                Ui.Commit(grid);if(!(grid.CurrentCell.Item is DataRowView start)||grid.CurrentColumn.DisplayIndex<2)return;
                int first=table.Rows.IndexOf(start.Row);int column=grid.CurrentColumn.DisplayIndex;
                var lines=CharacteristicFile.ParseClipboardRows(Clipboard.GetText());
                int width=lines.Max(l=>l.Length);int required=column-3+width;
                if(required>maxPoints)throw new InvalidOperationException("Вставка превышает "+maxPoints+" точек.");
                if(required>points){pointCount.Text=required.ToString();resize();}
                for(int r=0;r<Math.Min(3,lines.Length)&&first+r<branches*3;r++)
                {
                    var cells=lines[r];for(int c=0;c<cells.Length;c++)if(column+c>=3||(first+r)%3==0)table.Rows[first+r][column+c]=cells[c].ToString(CultureInfo.CurrentCulture);
                }
            };
            var context=new ContextMenu();foreach(var action in new[]{Tuple.Create("Копировать",copy),Tuple.Create("Вставить из Excel",paste),Tuple.Create("Очистить",clear)}){var item=new MenuItem{Header=action.Item1};item.Click+=(s,e)=>Ui.Run(action.Item2);context.Items.Add(item);}grid.ContextMenu=context;
            grid.PreviewKeyDown+=(s,e)=>{if((Keyboard.Modifiers&ModifierKeys.Control)!=0&&(e.Key==Key.V||e.Key==Key.C)){Ui.Run(e.Key==Key.V?paste:copy);e.Handled=true;}else if(e.Key==Key.Delete){Ui.Run(clear);e.Handled=true;}};
            commit=()=>
            {
                resize();model.Comment=comment.Text;for(int i=0;i<4;i++)model.Coefficients[i]=InputFiles3132.Parse(coefficients[i].Text,CultureInfo.CurrentCulture);
                model.Interpolation=int.Parse((string)mode.SelectedItem,CultureInfo.InvariantCulture);model.AdditionalValue=InputFiles3132.Parse(extra.Text,CultureInfo.CurrentCulture);
                model.Branches.Clear();
                for(int branch=0;branch<branches;branch++)
                {
                    int first=branch*3;if(string.IsNullOrWhiteSpace((string)table.Rows[first][2])||string.IsNullOrWhiteSpace((string)table.Rows[first][3]))continue;
                    int count=0;while(count<points&&!string.IsNullOrWhiteSpace((string)table.Rows[first][count+3]))count++;
                    var item=new CharacteristicBranch{Value=InputFiles3132.Parse((string)table.Rows[first][2],CultureInfo.CurrentCulture),Points=new double[3,count]};
                    for(int r=0;r<3;r++)for(int c=0;c<count;c++)item.Points[r,c]=InputFiles3132.Parse((string)table.Rows[first+r][c+3],CultureInfo.CurrentCulture);
                    model.Branches.Add(item);
                }
            };
            return tabs;
        }
    }
}
