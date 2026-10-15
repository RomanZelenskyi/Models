using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class CatalogEditor
    {
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct ClientRect { public int Left,Top,Right,Bottom; }
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr handle,out ClientRect rectangle);
        private static double ClientWidth(Window window)
        {
            var source=System.Windows.PresentationSource.FromVisual(window);
            if(source?.CompositionTarget==null||!GetClientRect(new System.Windows.Interop.WindowInteropHelper(window).Handle,out var rectangle))return 0;
            return source.CompositionTarget.TransformFromDevice.Transform(new Point(rectangle.Right-rectangle.Left,0)).X;
        }
        private static readonly DependencyProperty ChangeCheckProperty=DependencyProperty.RegisterAttached("ChangeCheck",typeof(Func<bool>),typeof(CatalogEditor));
        public static bool HasChanges(UIElement editor)=>(editor.GetValue(ChangeCheckProperty) as Func<bool>)?.Invoke()??false;
        private static string Snapshot(object value)
        {
            if(value==null)return "null";
            if(value is string text)return text.Length+":"+text;
            if(value.GetType().IsPrimitive||value is decimal||value.GetType().IsEnum)return Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture);
            if(value is IniFile ini)return string.Join(";",ini.SectionNames.OrderBy(s=>s).Select(section=>Snapshot(section)+Snapshot(ini.Entries(section).OrderBy(entry=>entry.Key).Select(entry=>entry.Key+"="+entry.Value))));
            if(value is System.Collections.IEnumerable items)return "["+string.Join(";",items.Cast<object>().Select(Snapshot))+"]";
            return "{"+string.Join(";",value.GetType().GetProperties().Where(p=>p.CanRead&&p.GetIndexParameters().Length==0).OrderBy(p=>p.Name).Select(p=>p.Name+"="+Snapshot(p.GetValue(value))))+"}";
        }
        public sealed class SelectionRow
        {
            public bool Enabled{get;set;} public int Number{get;set;} public string Symbol{get;set;} public string Name{get;set;} public string Node{get;set;}
        }
        public sealed class NumberRow { public int Number{get;set;} }
        public sealed class TextRow { public string Name{get;set;} }
        public sealed class IniRow { public string Section{get;set;} public string Key{get;set;} public string Value{get;set;} }
        public static UIElement Create(ApplicationSettings settings, string engine=null, bool classic=false, Action persist=null)
        {
            var model=new Unit3Model(settings.DataDirectory,settings.Language,engine??settings.Engine);var tabs=new TabControl();var apply=new List<Action>();
            Action<string,System.Collections.IEnumerable> readOnly=(title,items)=>Ui.Tab(tabs,title,new DataGrid{ItemsSource=items,IsReadOnly=true});
            if(!classic)Add(tabs,"Двигатели",model.Engines,100,apply);if(classic)Configurations(tabs,model.Configurations,apply);else Add(tabs,"Конфигурации",model.Configurations,31,apply);Add(tabs,"Задачи",model.Jobs,30,apply);Add(tabs,"Типы файлов",model.Includes,2000,apply);Add(tabs,"Разное",model.Misc,31,apply);Add(tabs,"Графики",model.Graphs,200,apply);Add(tabs,"Файлы 31–32",model.File3132,100,apply);
            var conditions=Ui.Grid(model.Conditions,"Number","Name");Ui.Tab(tabs,"Условия",conditions);
            Select(tabs,"Параметры",model.Parameters,model.SelectedParameters,apply);Select(tabs,"Управляющие параметры",model.Parameters,model.SelectedControls,apply);Select(tabs,"Характеристики",model.Characteristics,model.SelectedCharacteristics,apply);Select(tabs,"Файл 30",model.Parameters,model.SelectedFile30,apply);
            var order=model.IncludeOrder.Select(n=>new NumberRow{Number=n}).ToList();Add(tabs,"Порядок типов файлов",order,200,apply);apply.Add(()=>{model.IncludeOrder.Clear();model.IncludeOrder.AddRange(order.Select(n=>n.Number));});
            var dynamics=new TabControl();foreach(var pair in model.DynamicFactors){var list=pair.Value.Select(n=>new TextRow{Name=n}).ToList();Add(dynamics,"Задача "+pair.Key,list,30,apply);int key=pair.Key;apply.Add(()=>{model.DynamicFactors[key].Clear();model.DynamicFactors[key].AddRange(list.Select(n=>n.Name));});}Ui.Tab(tabs,"Динамические факторы",dynamics);
            // Input definitions expose every existing INI key and keep unrelated sections intact.
            EnsureInputDefaults(model.InputFiles);
            if(!model.Graphics.Entries("koef_use").Any())model.Graphics.Set("koef_use","koef","0");if(!model.Graphics.Entries("graphpoints").Any())model.Graphics.Set("graphpoints","grafix","1");
            Ini(tabs,"Описание входных файлов",model.InputFiles,apply,(section,key)=>!(section=="30"&&key.Equals("parametry",StringComparison.OrdinalIgnoreCase))&&!(section=="31_32"&&(key=="count"||System.Text.RegularExpressions.Regex.IsMatch(key,@"^[ne]\d+$"))));
            Ini(tabs,"Версия",model.Models,apply,(section,key)=>section.Equals("Interface",StringComparison.OrdinalIgnoreCase)&&(key=="version"||key=="new"));
            Ini(tabs,"Общие настройки графиков",model.Graphics,apply,(section,key)=>!System.Text.RegularExpressions.Regex.IsMatch(section,@"^graph\d+$",System.Text.RegularExpressions.RegexOptions.IgnoreCase)&&section!="graphics");
            var general=new StackPanel();var time=Ui.Text(general,"Номер параметра времени",model.TimeParameter.ToString());Ui.Tab(tabs,"Параметр времени",general);apply.Add(()=>{if(!int.TryParse(time.Text,out int n))throw new InvalidOperationException("Неверный номер времени.");model.TimeParameter=n;});
            foreach(var table in Descendants(tabs).OfType<DataGrid>())TrackCellEdits(table);
            Action synchronize=()=>{Ui.Commit(tabs);foreach(var edit in apply)edit();};synchronize();string baseline=Snapshot(model);Func<bool> changed=()=>{foreach(var edit in apply)edit();return Snapshot(model)!=baseline;};Action save=()=>{synchronize();model.Save();baseline=Snapshot(model);};if(!classic)return Ui.Editor(tabs,()=>{save();MessageBox.Show("Настройки модели сохранены.");});
            foreach(var bar in Descendants(tabs).OfType<StackPanel>().Where(panel=>panel.Children.OfType<Button>().Any(button=>Equals(button.Content,InterfaceText.Translate("Добавить")))).ToArray()){bar.Children.Add(Ui.Button("Применить",persist??save));}
            var timePage=(TabItem)tabs.Items[tabs.Items.Count-1];tabs.Items.Remove(timePage);foreach(TabItem page in tabs.Items){if(Equals(page.Header,"Типы файлов"))page.Header="Подключаемые файлы";if(Equals(page.Header,"Параметры"))page.Header="Параметры двигателя";if(Equals(page.Header,"Разное"))page.Header="Дополнительно";}
            var ordered=tabs.Items.Cast<TabItem>().ToList();string[] priority={"Конфигурации","Задачи","Подключаемые файлы","Параметры двигателя","Характеристики","Дополнительно","Графики"};tabs.Items.Clear();foreach(var page in ordered.OrderBy(item=>{int index=Array.IndexOf(priority,item.Header as string);return index<0?100:index;}))tabs.Items.Add(page);
            tabs.Template=(ControlTemplate)System.Windows.Markup.XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='TabControl'><DockPanel><ScrollViewer DockPanel.Dock='Top' HorizontalScrollBarVisibility='Auto' VerticalScrollBarVisibility='Disabled' CanContentScroll='False'><StackPanel Orientation='Horizontal' IsItemsHost='True'/></ScrollViewer><Border BorderBrush='#C8C8C8' BorderThickness='1' Background='#FAFAFA'><ContentPresenter x:Name='PART_SelectedContentHost' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' ContentSource='SelectedContent'/></Border></DockPanel></ControlTemplate>");
            var classicDock=new DockPanel{Tag=save};var footer=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(footer,Dock.Bottom);footer.Children.Add(new TextBlock{Text="Время",Margin=new Thickness(12,8,8,8)});general.Children.Remove(time);time.Width=120;time.MinWidth=0;footer.Children.Add(time);classicDock.Children.Add(footer);classicDock.Children.Add(tabs);classicDock.SetValue(ChangeCheckProperty,changed);return classicDock;
        }
        public static Window CreateWindow(ApplicationSettings settings)
        {
            var model=new Unit3Model(settings.DataDirectory,settings.Language,settings.Engine);
            var engines=new ObservableCollection<Unit3Engine>(model.Engines);
            var root=new Grid{Background=System.Windows.Media.Brushes.WhiteSmoke};root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(185),MinWidth=170});root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(5)});root.ColumnDefinitions.Add(new ColumnDefinition{MinWidth=340});
            var left=new DockPanel();root.Children.Add(left);
            var grid=new DataGrid{Name="EngineCatalog",ItemsSource=engines,AutoGenerateColumns=false,IsReadOnly=false,CanUserAddRows=false,CanUserDeleteRows=false,Margin=new Thickness(1),HeadersVisibility=DataGridHeadersVisibility.Column,SelectionMode=DataGridSelectionMode.Single};
            foreach(var column in new[]{new[]{"Номер","Code"},new[]{"Обозначение","Name"},new[]{"Наименование","Description"}})grid.Columns.Add(new DataGridTextColumn{Header=column[0],Binding=new Binding(column[1]),IsReadOnly=column[1]!="Description",MinWidth=column[1]=="Description"?110:column[1]=="Name"?90:50,Width=column[1]=="Description"?new DataGridLength(1,DataGridLengthUnitType.Star):DataGridLength.Auto});
            grid.SizeChanged+=(s,e)=>{double available=Math.Max(110,grid.ActualWidth-grid.Columns.Take(2).Sum(column=>column.ActualWidth)-2);grid.Columns[2].Width=new DataGridLength(available);};
            var leftButtons=new StackPanel{Margin=new Thickness(15,8,15,8)};DockPanel.SetDock(leftButtons,Dock.Bottom);left.Children.Add(leftButtons);left.Children.Add(grid);
            Action<double> resizePanels=leftWidth=>{double available=(double.IsNaN(root.Width)?root.ActualWidth:root.Width)-5;var leftColumn=root.ColumnDefinitions[0];double limit=Math.Max(leftColumn.MinWidth,available-root.ColumnDefinitions[2].MinWidth);leftColumn.MaxWidth=limit;leftColumn.Width=new GridLength(Math.Max(leftColumn.MinWidth,Math.Min(leftWidth,limit)));root.ColumnDefinitions[2].Width=new GridLength(1,GridUnitType.Star);};root.SizeChanged+=(s,e)=>resizePanels(root.ColumnDefinitions[0].Width.Value);
            var splitter=new System.Windows.Controls.Primitives.Thumb{Name="SettingsSplitter",Width=5,HorizontalAlignment=HorizontalAlignment.Stretch,Cursor=System.Windows.Input.Cursors.SizeWE,Focusable=true};var splitterTemplate=new ControlTemplate(typeof(System.Windows.Controls.Primitives.Thumb));var splitterBorder=new FrameworkElementFactory(typeof(Border));splitterBorder.SetValue(Border.BackgroundProperty,System.Windows.Media.Brushes.LightGray);splitterTemplate.VisualTree=splitterBorder;splitter.Template=splitterTemplate;splitter.DragDelta+=(s,e)=>{resizePanels(root.ColumnDefinitions[0].ActualWidth+e.HorizontalChange);e.Handled=true;};splitter.KeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Left||e.Key==System.Windows.Input.Key.Right){resizePanels(root.ColumnDefinitions[0].ActualWidth+(e.Key==System.Windows.Input.Key.Left?-10:10));e.Handled=true;}};Grid.SetColumn(splitter,1);root.Children.Add(splitter);
            var right=new DockPanel{Margin=new Thickness(5)};Grid.SetColumn(right,2);root.Children.Add(right);
            var bottom=new DockPanel();DockPanel.SetDock(bottom,Dock.Bottom);var ok=Ui.Button("OK",()=>{});ok.Width=75;ok.HorizontalAlignment=HorizontalAlignment.Right;bottom.Children.Add(ok);right.Children.Add(bottom);
            var host=new ContentControl();right.Children.Add(host);
            var window=Ui.Dialog("Настройки проекта",root,880,646);window.MinWidth=550;window.MinHeight=350;Action fitClient=()=>{double width=ClientWidth(window);if(width<=0)return;root.Width=width;resizePanels(root.ColumnDefinitions[0].Width.Value);};window.SourceInitialized+=(s,e)=>fitClient();window.SizeChanged+=(s,e)=>fitClient();var buttonStyle=new Style(typeof(Button),(Style)Application.Current.FindResource(typeof(Button)));buttonStyle.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(6,2,6,2)));buttonStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty,new Thickness(4,2,4,2)));buttonStyle.Setters.Add(new Setter(FrameworkElement.HeightProperty,24d));window.Resources.Add(typeof(Button),buttonStyle);
            TrackCellEdits(grid);
            UIElement editor=null;Unit3Engine selected=null;bool restoring=false;string enginesBaseline=Snapshot(engines);
            Action refresh=()=>{};
            Action save=()=>{if(editor==null)return;Ui.Commit(grid);((Action)((FrameworkElement)editor).Tag)();var latest=new Unit3Model(settings.DataDirectory,settings.Language,selected.Name);latest.Engines.Clear();latest.Engines.AddRange(engines);latest.Save();grid.Items.Refresh();enginesBaseline=Snapshot(engines);refresh();};

            ok.Click+=(s,e)=>Ui.Run(()=>{save();window.Close();});
            grid.SelectionChanged+=(s,e)=>
            {
                if(restoring)return;var next=grid.SelectedItem as Unit3Engine;if(next==null)return;
                if(editor!=null&&selected!=next&&(HasChanges(editor)||Snapshot(engines)!=enginesBaseline)){var answer=MessageBox.Show(window,"Сохранить настройки текущего проекта перед переключением?","Настройки проекта",MessageBoxButton.YesNoCancel);if(answer==MessageBoxResult.Cancel){restoring=true;grid.SelectedItem=selected;restoring=false;return;}if(answer==MessageBoxResult.Yes){try{save();}catch(Exception error){MessageBox.Show(window,error.Message);restoring=true;grid.SelectedItem=selected;restoring=false;return;}}}
                try{editor=Create(settings,next.Name,true,save);selected=next;host.Content=editor;refresh();}catch(Exception error){MessageBox.Show(window,error.Message);}
            };
            Action reload=()=>{var refreshed=new Unit3Model(settings.DataDirectory,settings.Language,settings.Engine);restoring=true;engines.Clear();foreach(var item in refreshed.Engines)engines.Add(item);restoring=false;editor=null;selected=null;grid.SelectedItem=engines.FirstOrDefault(item=>item.Name==settings.Engine)??engines.FirstOrDefault();};
            leftButtons.Children.Add(Ui.Button("Добавить",()=>{save();if(SettingsWindows.CreateEngine(settings))reload();}));
            leftButtons.Children.Add(Ui.Button("Импортировать",()=>{save();SettingsWindows.ImportEngine(settings);reload();}));
            leftButtons.Children.Add(Ui.Button("Удалить",()=>{if(selected==null)return;if(MessageBox.Show(window,"Удалить проект из списка? Файлы двигателя останутся на диске.","Настройки проекта",MessageBoxButton.OKCancel)!=MessageBoxResult.OK)return;var latest=new Unit3Model(settings.DataDirectory,settings.Language,selected.Name);latest.Engines.RemoveAll(item=>item.Name==selected.Name);latest.Save();reload();}));
            leftButtons.Children.Add(Ui.Button("Применить",save));
            refresh=()=>{bool dirty=false;if(editor!=null){try{dirty=HasChanges(editor)||Snapshot(engines)!=enginesBaseline;}catch{dirty=true;}}foreach(var button in Descendants(root).OfType<Button>().Where(b=>Equals(b.Content,InterfaceText.Translate("Применить"))))button.IsEnabled=dirty;};
            bool refreshPending=false;Action schedule=()=>{if(refreshPending)return;refreshPending=true;root.Dispatcher.BeginInvoke(new Action(()=>{refreshPending=false;refresh();}),System.Windows.Threading.DispatcherPriority.DataBind);};
            root.AddHandler(TextBox.TextChangedEvent,new TextChangedEventHandler((s,e)=>schedule()));root.AddHandler(System.Windows.Controls.Primitives.ToggleButton.CheckedEvent,new RoutedEventHandler((s,e)=>schedule()));root.AddHandler(System.Windows.Controls.Primitives.ToggleButton.UncheckedEvent,new RoutedEventHandler((s,e)=>schedule()));root.AddHandler(Button.ClickEvent,new RoutedEventHandler((s,e)=>schedule()));root.AddHandler(System.Windows.Input.Keyboard.KeyUpEvent,new System.Windows.Input.KeyEventHandler((s,e)=>schedule()));root.Loaded+=(s,e)=>refresh();
            grid.SelectedItem=engines.FirstOrDefault(item=>item.Name==settings.Engine)??engines.FirstOrDefault();
            InterfaceText.Apply(root);return window;
        }
        private static void TrackCellEdits(DataGrid grid)
        {
            Action<DataGridColumn> track=column=>{if(column is DataGridBoundColumn bound&&bound.Binding is Binding binding){binding.UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged;}};
            foreach(var column in grid.Columns)track(column);
            grid.AutoGeneratingColumn+=(s,e)=>track(e.Column);
        }
        private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            yield return root;
            foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())foreach(var item in Descendants(child))yield return item;
        }
        private static void Configurations(TabControl tabs,List<Unit3Choice> values,List<Action> apply)
        {
            var rows=new ObservableCollection<Unit3Choice>(values);var grid=new DataGrid{Name="ConfigurationsGrid",ItemsSource=rows,AutoGenerateColumns=false,CanUserAddRows=false,CanUserDeleteRows=false,Margin=new Thickness(2),HeadersVisibility=DataGridHeadersVisibility.Column,Background=System.Windows.Media.Brushes.White,HorizontalGridLinesBrush=System.Windows.Media.Brushes.WhiteSmoke,VerticalGridLinesBrush=System.Windows.Media.Brushes.WhiteSmoke};
            grid.Columns.Add(new DataGridCheckBoxColumn{Header="",Binding=new Binding("Enabled"),Width=30});grid.Columns.Add(new DataGridTextColumn{Header="Номер",Binding=new Binding("Number"),IsReadOnly=true,Width=45});grid.Columns.Add(new DataGridTextColumn{Header="Наименование",Binding=new Binding("Name"),Width=new DataGridLength(1,DataGridLengthUnitType.Star)});
            var dock=new DockPanel();
            var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(footer,Dock.Bottom);footer.Children.Add(Ui.Button("Добавить",()=>{Ui.Commit(grid);if(rows.Count>=31)throw new InvalidOperationException("Максимум 31 конфигурация.");var row=new Unit3Choice{Number=rows.Count,Name=""};rows.Add(row);grid.SelectedItem=row;}));footer.Children.Add(Ui.Button("Удалить",()=>{Ui.Commit(grid);if(grid.SelectedItem is Unit3Choice row){rows.Remove(row);for(int i=0;i<rows.Count;i++)rows[i].Number=i;grid.Items.Refresh();}}));dock.Children.Add(footer);dock.Children.Add(grid);
Ui.Tab(tabs,"Конфигурации",dock);apply.Add(()=>{values.Clear();values.AddRange(rows);});
        }
        private static void Add<T>(TabControl tabs,string title,List<T> values,int capacity,List<Action> apply) where T:class,new()
        {
            var collection=new ObservableCollection<T>(values);var grid=Ui.Grid(collection);var dock=new DockPanel();var bar=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(bar,Dock.Bottom);dock.Children.Add(bar);dock.Children.Add(grid);
            bar.Children.Add(Ui.Button("Добавить",()=>{if(collection.Count>=capacity)throw new InvalidOperationException("Достигнут предел записей.");var item=new T();var id=typeof(T).GetProperty("Number")??typeof(T).GetProperty("Code");if(id!=null)id.SetValue(item,collection.Count==0?typeof(T)==typeof(Unit3Choice)?0:1:collection.Max(x=>(int)id.GetValue(x))+1);collection.Add(item);}));
            bar.Children.Add(Ui.Button("Удалить",()=>{if(grid.SelectedItem is T item&&MessageBox.Show("Удалить запись?","Модель",MessageBoxButton.OKCancel)==MessageBoxResult.OK)collection.Remove(item);}));
            foreach(int delta in new[]{-1,1})bar.Children.Add(Ui.Button(delta<0?"Выше":"Ниже",()=>{int i=grid.SelectedIndex,j=i+delta;if(i>=0&&j>=0&&j<collection.Count){collection.Move(i,j);grid.SelectedIndex=j;}}));
            Ui.Tab(tabs,title,dock);apply.Add(()=>{values.Clear();values.AddRange(collection);});
        }
        private static void Select(TabControl tabs,string title,List<Unit3Parameter> catalog,HashSet<int> selected,List<Action> apply)
        {
            var rows=catalog.Select(p=>new SelectionRow{Enabled=selected.Contains(p.Number),Number=p.Number,Symbol=p.Symbol,Name=p.Name,Node=p.Node}).ToArray();var grid=Ui.Grid(rows,"Number","Symbol","Name","Node");var dock=new DockPanel();var filter=new TextBox();DockPanel.SetDock(filter,Dock.Top);dock.Children.Add(filter);dock.Children.Add(grid);
            filter.TextChanged+=(s,e)=>CollectionViewSource.GetDefaultView(grid.ItemsSource).Filter=item=>{var row=(SelectionRow)item;return (row.Number+" "+row.Symbol+" "+row.Name+" "+row.Node).IndexOf(filter.Text,StringComparison.CurrentCultureIgnoreCase)>=0;};
            Ui.Tab(tabs,title,dock);apply.Add(()=>{selected.Clear();foreach(var row in rows.Where(r=>r.Enabled))selected.Add(row.Number);});
        }
        private static void Ini(TabControl tabs,string title,IniFile ini,List<Action> apply,Func<string,string,bool> include=null)
        {
            var rows=ini.SectionNames.SelectMany(section=>ini.Entries(section).Where(p=>include==null||include(section,p.Key)).Select(p=>new IniRow{Section=section,Key=p.Key,Value=p.Value})).ToList();var grid=Ui.Grid(rows,"Section","Key");Ui.Tab(tabs,title,grid);apply.Add(()=>{foreach(var row in rows)ini.Set(row.Section,row.Key,row.Value??"");});
        }
        private static void EnsureInputDefaults(IniFile ini)
        {
            Action<string,string,string> field=(section,key,value)=>{if(!ini.Entries(section).Any(e=>e.Key.Equals(key,StringComparison.OrdinalIgnoreCase)))ini.Set(section,key,value);};
            Action<string,string,int,string> range=(section,prefix,count,value)=>{for(int i=1;i<=count;i++)field(section,prefix+i,value);};
            range("28","col",20,"");range("28","row",10,"");range("28","nrow",20,"0");range("28","ce",20,"0");range("28","re",10,"0");range("28","ne",3,"0");range("28","n",3,"");field("28","n28count","10");
            range("24","n",3,"");range("24","ne",4,"0");for(int i=11;i<=25;i++){field("24","n"+i,"");field("24","ne"+i,"0");}
            range("41","ne",9,"0");for(int j=0;j<7;j++)for(int i=1;i<=9;i++)field("41","n"+j+i,"");range("51","n",4,"");range("51","ne",4,"0");for(int j=1;j<=3;j++)for(int i=1;i<=5;i++)field("2940","n"+j+i,"0");for(int i=0;i<4;i++){field("31_32","preg"+i,"0");field("preg","preg"+i,"0");}
            field("rez","rez","0");field("paramdat","param","10");field("31_32","r","0.5");field("31_32","r1","0");field("31_32","r2","1");
            foreach(var section in new[]{"1","17","31","32","41","42","43","44","48","49","50","281","282","1210","1220","1230","1240","1250","300","55","56"})field(section,"new","0");
        }
    }
}











