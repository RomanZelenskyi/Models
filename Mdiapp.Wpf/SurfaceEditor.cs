using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class SurfaceEditor
    {
        public static UIElement Create(ApplicationSettings settings,string path,bool unit37=false)
        {
            var model=InputSurfaceFile.Load(path,unit37:unit37);string unit=unit37?"37":"35";var tabs=new TabControl();var setup=new StackPanel{Margin=new Thickness(8)};Ui.Tab(tabs,"Размеры, масштабы и оси",new ScrollViewer{Content=setup});
            Func<string,string,string> label=(key,fallback)=>settings.Menu.Get("files",unit37?key.Replace("f42","f44"):key,fallback);
            var comment=Ui.Text(setup,"Комментарий",model.Comment);var scalePanel=new WrapPanel();setup.Children.Add(scalePanel);var scales=new TextBox[model.ScaleCount];
            for(int i=0;i<model.ScaleCount;i++)
            {
                var group=new StackPanel{Width=330,Margin=new Thickness(4)};scalePanel.Children.Add(group);scales[i]=Ui.Text(group,"Масштаб "+(i+1),model.Scales[i].ToString(CultureInfo.CurrentCulture));scales[i].Name="Scale"+unit+"_"+i;
                string picture=Path.Combine(settings.DataDirectory,settings.Engine,settings.Language,model.Type+"_img"+(i+1)+".bmp");if(File.Exists(picture)){var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.UriSource=new Uri(picture,UriKind.Absolute);bitmap.EndInit();bitmap.Freeze();group.Children.Add(new Image{Source=bitmap,Height=50,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(8,3,8,0)});}
            }
            int[] dimensions={model.X.Length,model.Y.Length,model.Temperatures.Length,model.BranchCount};string[] keys={"f42l1","f42l4","f42l5","f42l6"};string[] fallback={"Точек X (1–100)","Точек Y (1–100)","Температур (1–30)","Ветвей (1–50)"};
            var sizes=dimensions.Select((value,i)=>Ui.Text(setup,label(keys[i],fallback[i]),value.ToString())).ToArray();for(int i=0;i<4;i++)sizes[i].Name="Size"+unit+"_"+i;
            var axes=new StackPanel();setup.Children.Add(axes);var surface=new DockPanel();Ui.Tab(tabs,"Основная таблица",surface);var selectors=new StackPanel();DockPanel.SetDock(selectors,Dock.Top);surface.Children.Add(selectors);
            var temperature=Ui.Choices(selectors,label("f42r1","Температура"),new string[0]);temperature.Name="Temperature"+unit;var branch=Ui.Choices(selectors,label("f42n6","Ветвь"),new string[0]);branch.Name="Branch"+unit;
            var temperatureValue=Ui.Text(selectors,label("f42l7","Значение температуры"),"");temperatureValue.Name="TemperatureValue"+unit;var branchValue=Ui.Text(selectors,label("f42n6","Значение ветви"),"");branchValue.Name="BranchValue"+unit;
            if(unit37)
            {
                Action<Panel,UIElement> hide=(parent,element)=>{element.Visibility=Visibility.Collapsed;parent.Children[parent.Children.IndexOf(element)-1].Visibility=Visibility.Collapsed;};
                hide(setup,sizes[3]);hide(selectors,branch);hide(selectors,branchValue);
            }
            var holder=new ContentControl();surface.Children.Add(holder);Action commitSurface=()=>{};var axisCommits=new List<Action>();bool changing=false;
            Action show=()=>
            {
                int t=temperature.SelectedIndex,b=branch.SelectedIndex;if(t<0||b<0)return;temperatureValue.Text=model.Temperatures[t].ToString(CultureInfo.CurrentCulture);branchValue.Text=model.BranchValues[t,b].ToString(CultureInfo.CurrentCulture);
                holder.Content=Matrix(model.Y.Length,model.X.Length,(r,c)=>model.Values[t,b,r,c],(r,c,v)=>model.Values[t,b,r,c]=v,r=>model.Y[r].ToString(CultureInfo.CurrentCulture),c=>model.X[c].ToString(CultureInfo.CurrentCulture),out Action commit);
                commitSurface=()=>{commit();model.Temperatures[t]=InputFiles3132.Parse(temperatureValue.Text,CultureInfo.CurrentCulture);model.BranchValues[t,b]=InputFiles3132.Parse(branchValue.Text,CultureInfo.CurrentCulture);};
            };
            Action refresh=()=>
            {
                changing=true;temperature.ItemsSource=Enumerable.Range(1,model.Temperatures.Length).Select(i=>"Температура "+i).ToArray();branch.ItemsSource=Enumerable.Range(1,model.BranchCount).Select(i=>"Ветвь "+i).ToArray();temperature.SelectedIndex=0;branch.SelectedIndex=0;changing=false;show();
                axes.Children.Clear();axisCommits.Clear();for(int i=0;i<2;i++){var data=i==0?model.X:model.Y;axes.Children.Add(new TextBlock{Text=label(i==0?"f42n1":"f42n2",i==0?"X":"Y"),FontWeight=FontWeights.Bold,Margin=new Thickness(0,12,0,4)});axes.Children.Add(Matrix(1,data.Length,(r,c)=>data[c],(r,c,v)=>data[c]=v,r=>"Значение",c=>(c+1).ToString(),out Action commit));axisCommits.Add(commit);}
            };
            temperature.SelectionChanged+=(s,e)=>{if(changing)return;commitSurface();show();};branch.SelectionChanged+=(s,e)=>{if(changing)return;commitSurface();show();};
            Action[] correctionCommits=new Action[model.CorrectionCount];Action[] correctionRefresh=new Action[model.CorrectionCount];var correctionSizes=new TextBox[model.CorrectionCount,2];
            for(int i=0;i<model.CorrectionCount;i++)
            {
                int index=i;var correction=model.Corrections[i];var panel=new DockPanel();var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);Ui.Tab(tabs,unit37?"Переходные коэффициенты":label(i==0?"f42l10":"f42l11","Переходные коэффициенты "+(i+1)),panel);
                correctionSizes[i,0]=Ui.Text(top,label(i==0?"f42l2":"f42l8","Число ветвей (1–30)"),correction.BranchCount.ToString());correctionSizes[i,0].Name="CorrectionBranches"+unit+"_"+i;
                correctionSizes[i,1]=Ui.Text(top,label(i==0?"f42l3":"f42l9","Число точек (1–100)"),correction.Arguments.Length.ToString());correctionSizes[i,1].Name="CorrectionPoints"+unit+"_"+i;
                var content=new ContentControl();panel.Children.Add(content);correctionCommits[i]=()=>{};
                correctionRefresh[i]=()=>content.Content=Matrix(1+correction.BranchCount*2,correction.Arguments.Length,(r,c)=>r==0?correction.Arguments[c]:correction.Values[r-1,c],(r,c,v)=>{if(r==0)correction.Arguments[c]=v;else correction.Values[r-1,c]=v;},r=>r==0?label(index==0?"f42n3":"f42n7","Аргумент"):label("f42n"+(index==0?(r%2==1?4:5):(r%2==1?8:9)),r%2==1?"k":"T")+" "+((r+1)/2),c=>(c+1).ToString(),out correctionCommits[index]);
                top.Children.Add(Ui.Button("Применить размеры",()=>{correctionCommits[index]();correction.Resize(Number(correctionSizes[index,0],30),Number(correctionSizes[index,1],100));correctionRefresh[index]();}));correctionRefresh[i]();
            }
            Action commitAll=()=>{Ui.Commit(tabs);commitSurface();foreach(var commit in axisCommits)commit();foreach(var commit in correctionCommits)commit();};
            setup.Children.Insert(setup.Children.Count-1,Ui.Button("Применить размеры основной таблицы",()=>{int x=Number(sizes[0],100),y=Number(sizes[1],100),t=Number(sizes[2],30),b=Number(sizes[3],50);commitAll();model.Resize(x,y,t,b);refresh();}));refresh();
            Action<string> save=target=>
            {
                int x=Number(sizes[0],100),y=Number(sizes[1],100),t=Number(sizes[2],30),b=Number(sizes[3],50);var correctionDimensions=Enumerable.Range(0,model.CorrectionCount).Select(i=>new[]{Number(correctionSizes[i,0],30),Number(correctionSizes[i,1],100)}).ToArray();commitAll();
                model.Resize(x,y,t,b);for(int i=0;i<model.CorrectionCount;i++){model.Corrections[i].Resize(correctionDimensions[i][0],correctionDimensions[i][1]);}for(int i=0;i<model.ScaleCount;i++)model.Scales[i]=InputFiles3132.Parse(scales[i].Text,CultureInfo.CurrentCulture);model.Comment=comment.Text;model.Save(target);path=target;refresh();foreach(var action in correctionRefresh)action();
            };
            return Ui.Editor(tabs,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
        private static int Number(TextBox field,int maximum){if(!int.TryParse(field.Text,out int number)||number<1||number>maximum)throw new InvalidOperationException("Количество должно быть от 1 до "+maximum+".");return number;}
        internal static UIElement Matrix(int rows,int columns,Func<int,int,double> get,Action<int,int,double> set,Func<int,string> rowLabel,Func<int,string> columnLabel,out Action commit)
        {
            var table=new DataTable();table.Columns.Add("Row",typeof(string));for(int col=0;col<columns;col++)table.Columns.Add("Value"+col,typeof(string));for(int row=0;row<rows;row++)table.Rows.Add(new object[]{rowLabel(row)}.Concat(Enumerable.Range(0,columns).Select(col=>(object)get(row,col).ToString(CultureInfo.CurrentCulture))).ToArray());
            var grid=new DataGrid{ItemsSource=table.DefaultView,AutoGenerateColumns=false,CanUserAddRows=false,CanUserDeleteRows=false,CanUserReorderColumns=false,FrozenColumnCount=1,SelectionUnit=DataGridSelectionUnit.CellOrRowHeader,MinHeight=80,MaxHeight=600};
            grid.Columns.Add(new DataGridTextColumn{Header="№ / аргумент",Binding=new Binding("Row"),IsReadOnly=true,CanUserSort=false,Width=150});for(int col=0;col<columns;col++)grid.Columns.Add(new DataGridTextColumn{Header=columnLabel(col),Binding=new Binding("Value"+col),Width=100,CanUserSort=false});
            commit=()=>{Ui.Commit(grid);for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)set(row,col,InputFiles3132.Parse((string)table.Rows[row][col+1],CultureInfo.CurrentCulture));};
            Action copy=()=>{if(grid.CurrentCell.Item is DataRowView row&&grid.CurrentColumn.DisplayIndex>0)Clipboard.SetText(Convert.ToString(row[grid.CurrentColumn.DisplayIndex]));};
            Action clear=()=>{Ui.Commit(grid);foreach(var cell in grid.SelectedCells)if(cell.Item is DataRowView row&&cell.Column.DisplayIndex>0)row[cell.Column.DisplayIndex]="";};
            Action paste=()=>{Ui.Commit(grid);if(!(grid.CurrentCell.Item is DataRowView first)||grid.CurrentColumn.DisplayIndex<1)return;int row=table.Rows.IndexOf(first.Row),col=grid.CurrentColumn.DisplayIndex;var values=InputSurfaceFile.ParseClipboard(Clipboard.GetText());if(row+values.Length>rows||values.Any(line=>col-1+line.Length>columns))throw new InvalidOperationException("Вставка выходит за текущие размеры таблицы.");for(int r=0;r<values.Length;r++)for(int c=0;c<values[r].Length;c++)table.Rows[row+r][col+c]=values[r][c].ToString(CultureInfo.CurrentCulture);};
            var context=new ContextMenu();foreach(var item in new[]{Tuple.Create("Копировать",copy),Tuple.Create("Вставить из Excel",paste),Tuple.Create("Очистить",clear)}){var entry=new MenuItem{Header=item.Item1};entry.Click+=(s,e)=>Ui.Run(item.Item2);context.Items.Add(entry);}grid.ContextMenu=context;grid.PreviewKeyDown+=(s,e)=>{if((Keyboard.Modifiers&ModifierKeys.Control)!=0&&(e.Key==Key.C||e.Key==Key.V)){Ui.Run(e.Key==Key.C?copy:paste);e.Handled=true;}else if(e.Key==Key.Delete){Ui.Run(clear);e.Handled=true;}};return grid;
        }
    }
}
