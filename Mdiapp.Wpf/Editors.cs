using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class Editors
    {
        private static double Parse(string text) => InputFiles3132.Parse(text, CultureInfo.CurrentCulture);
        public static UIElement Project(string path)
        {
            var model = LegacyProject.Load(path); var panel = new StackPanel(); var comment = Ui.Text(panel, "Комментарий", model.Remark);
            panel.Children.Add(new TextBlock { Text = "Ключ: " + model.ProjectKey + " | Код: " + model.FileCode, Margin = new Thickness(8) });
            panel.Children.Add(new ListBox { ItemsSource = model.IncludedFiles, Margin = new Thickness(8), Height = 350 });
            Action save = () => { string target = Ui.Save(path, "INM|*.inm", "inm"); if (target != null) { model.Remark = comment.Text; model.Save(target); } };
            return Ui.Editor(panel, save);
        }
        public static UIElement Series()
        {
            var dock = new DockPanel(); var panel = new StackPanel { Width = 300 }; DockPanel.SetDock(panel, Dock.Left); dock.Children.Add(panel);
            var mode = Ui.Choices(panel, "Закон изменения параметра", new[] {"Постоянное значение", "Приращение", "a+t·k1+k2·t^p"}); mode.SelectedIndex = 0;
            var fields = new[] {"a", "Приращение", "k1", "k2", "p", "Число точек", "Шаг времени"}.Select(label => Ui.Text(panel, label, label == "Число точек" ? "10" : label == "Шаг времени" ? "1" : "0")).ToArray();
            var grid = new DataGrid { IsReadOnly = true }; dock.Children.Add(grid);
            panel.Children.Add(Ui.Button("Рассчитать", () =>
            {
                if (!int.TryParse(fields[5].Text, out int count) || count < 1 || count > 10000) throw new InvalidOperationException("Число точек: 1–10000.");
                var times = Enumerable.Range(0, count).Select(i => i * Parse(fields[6].Text)).ToArray();
                var values = ParameterSeries.Calculate(times, (SeriesMode)mode.SelectedIndex, Parse(fields[0].Text), Parse(fields[1].Text), Parse(fields[2].Text), Parse(fields[3].Text), Parse(fields[4].Text));
                grid.ItemsSource = times.Select((time,i) => new {Время = time, Значение = values[i]}).ToArray();
            })); return dock;
        }
        public static UIElement File51(ApplicationSettings settings,string path)
        {
            var model = InputFile51.Load(path, CultureInfo.CurrentCulture); var panel = new StackPanel(); var comment = Ui.Text(panel, "Комментарий", model.Comment);
            var defs=IniFile.Load(Path.Combine(settings.DataDirectory,settings.Engine,settings.Language,"inputfiles.ini"));
            var values = model.Values.Select((v,i) => Ui.Text(panel, defs.Get("51","n"+(i+1),"Параметр " + (i+1)), v.ToString(CultureInfo.CurrentCulture))).ToArray();for(int i=0;i<4;i++)values[i].IsEnabled=defs.GetInt("51","ne"+(i+1),1)!=0;
            Action<string> save = target => { model.Comment = comment.Text; for (int i=0;i<4;i++) model.Values[i] = Parse(values[i].Text); model.Save(target, CultureInfo.CurrentCulture); path=target; };
            return Ui.Editor(panel, () => save(path), () => { var target=Ui.Save(path); if(target!=null) save(target); });
        }
        public static UIElement File24(ApplicationSettings settings, string path)
        {
            var defs = IniFile.Load(Path.Combine(settings.DataDirectory, settings.Engine, settings.Language, "inputfiles.ini"));
            var model = InputFile24.Load(path, defs, settings.Menu);
            var panel = new StackPanel(); var comment = Ui.Text(panel, "Комментарий", model.Comment);
            panel.Children.Add(new TextBlock { Text = model.GroupCaption, Margin = new Thickness(8) });
            var values = model.Values.Select((v, i) => Ui.Text(panel, string.IsNullOrEmpty(model.Names[i]) ? "Параметр " + (i + 1) : model.Names[i], v.ToString(CultureInfo.CurrentCulture))).ToArray();
            for (int i = 0; i < 4; i++) values[i].IsEnabled = model.Enabled[i];
            var additional = new TextBox[15];
            for (int i = 0; i < 15; i++) if (model.AdditionalEnabled[i]) additional[i] = Ui.Text(panel, model.AdditionalNames[i], model.AdditionalValues[i].ToString(CultureInfo.CurrentCulture));
            Action<string> save = target =>
            {
                model.Comment = comment.Text;
                for (int i = 0; i < 4; i++) model.Values[i] = Parse(values[i].Text);
                for (int i = 0; i < 15; i++) if (additional[i] != null) model.AdditionalValues[i] = Parse(additional[i].Text);
                model.Save(target); path = target;
            };
            return Ui.Editor(new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }, () => save(path), () => { var target = Ui.Save(path); if (target != null) save(target); });
        }
        public static UIElement File41(ApplicationSettings settings, string path)
        {
            var model = InputFile41.Load(path); var catalog = new Unit3Model(settings.DataDirectory, settings.Language, settings.Engine);
            var defs = catalog.InputFiles; var menu = settings.Menu;
            var panel = new DockPanel(); var top = new StackPanel(); DockPanel.SetDock(top, Dock.Top);
            var comment = Ui.Text(top, "Комментарий", model.Comment); panel.Children.Add(top);
            var tabs = new TabControl(); panel.Children.Add(tabs); var fields = new TextBox[6,9]; var selectors = new ComboBox[9];
            for (int row = 0; row < 7; row++)
            {
                var group = new StackPanel(); Ui.Tab(tabs, menu.Get("files", "n" + (row + 18), "Группа " + (row + 1)), new ScrollViewer { Content = group, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
                for (int col = 0; col < 9; col++)
                {
                    string label = defs.Get("41", "n" + row + (col + 1), "Параметр " + (col + 1)); bool enabled = defs.GetInt("41", "ne" + (col + 1)) == 1;
                    if (row < 6) { fields[row,col] = Ui.Text(group, label, model.Values[row,col].ToString(CultureInfo.CurrentCulture)); fields[row,col].IsEnabled = enabled; }
                    else
                    {
                        var choices = catalog.Parameters.Where(p => p.Number >= 51 && p.Number <= 100 && !string.IsNullOrEmpty(p.Symbol) && settings.IsParameterActive(p.Number)).Select(p => new ParameterOption { Number = p.Number, Name = p.Symbol + " " + p.Name }).ToList();
                        choices.Insert(0, new ParameterOption { Number = 0, Name = "Без параметра" });
                        if (!choices.Any(p => p.Number == model.Parameters[col])) choices.Add(new ParameterOption { Number = model.Parameters[col], Name = "Из файла (нет в каталоге)" });
                        selectors[col] = Ui.Choices(group, label, choices); selectors[col].SelectedItem = choices.Single(p => p.Number == model.Parameters[col]); selectors[col].IsEnabled = enabled;
                    }
                }
            }
            Action<string> save = target =>
            {
                model.Comment = comment.Text;
                for (int row = 0; row < 6; row++) for (int col = 0; col < 9; col++) model.Values[row,col] = Parse(fields[row,col].Text);
                for (int col = 0; col < 9; col++) model.Parameters[col] = ((ParameterOption)selectors[col].SelectedItem).Number;
                model.Save(target); path = target;
            };
            return Ui.Editor(panel, () => save(path), () => { var target = Ui.Save(path); if (target != null) save(target); });
        }
        private static DataTable Table(int rows, int columns)
        {
            var table = new DataTable(); for(int i=0;i<columns;i++) table.Columns.Add("Value" + (i+1), typeof(double)); for(int i=0;i<rows;i++) table.Rows.Add(Enumerable.Repeat<object>(0d,columns).ToArray()); return table;
        }
        public static UIElement File28(ApplicationSettings settings, string path)
        {
            var defs=IniFile.Load(Path.Combine(settings.DataDirectory,settings.Engine,settings.Language,"inputfiles.ini")); var model=InputFile28.Load(path,defs);
            var panel=File28Content(model,out Action commit);
            Action<string> save=target=>{commit();model.Save(target);path=target;};return Ui.Editor(panel,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
        internal static UIElement File28Content(InputFile28 model,out Action commit)
        {
            var panel=new DockPanel(); var top=new StackPanel(); DockPanel.SetDock(top,Dock.Top); var comment=Ui.Text(top,"Комментарий",model.Comment); panel.Children.Add(top);
            var bottom=new StackPanel(); DockPanel.SetDock(bottom,Dock.Bottom); var power=model.Power.Select((v,i)=>Ui.Text(bottom,model.PowerNames[i],v.ToString(CultureInfo.CurrentCulture))).ToArray(); for(int i=0;i<3;i++) power[i].IsEnabled=model.PowerEnabled[i]; panel.Children.Add(bottom);
            var table=new DataTable(); table.Columns.Add("RowName",typeof(string)); for(int i=0;i<model.Columns.Count;i++) table.Columns.Add("Value"+i,typeof(double));
            foreach(var row in model.Rows) table.Rows.Add(new object[]{row.Name}.Concat(model.Columns.Select(c=>(object)model.Values[row.PhysicalIndex,c.PhysicalIndex])).ToArray());
            var grid=new DataGrid{ItemsSource=table.DefaultView,FrozenColumnCount=1}; grid.AutoGeneratingColumn+=(s,e)=>{e.Column.IsReadOnly=e.PropertyName=="RowName"; e.Column.Header=e.PropertyName=="RowName"?"Источник":model.Columns[table.Columns[e.PropertyName].Ordinal-1].Name;e.Column.CanUserSort=false;}; panel.Children.Add(grid);
            commit=()=>{Ui.Commit(panel);model.Comment=comment.Text;for(int r=0;r<model.Rows.Count;r++)for(int c=0;c<model.Columns.Count;c++)model.Values[model.Rows[r].PhysicalIndex,model.Columns[c].PhysicalIndex]=(double)table.Rows[r][c+1];for(int i=0;i<3;i++)model.Power[i]=Parse(power[i].Text);};
            return panel;
        }
        public static UIElement File30(ApplicationSettings settings,string path)
        {
            var model=InputFile30.Load(path);var catalog=new Unit3Model(settings.DataDirectory,settings.Language,settings.Engine);var tabs=new TabControl();var overview=new StackPanel();var comment=Ui.Text(overview,"Комментарий",model.Comment);var selectors=new ComboBox[6];
            for(int i=0;i<6;i++)
            {
                var choices=catalog.Parameters.Where(p=>catalog.SelectedFile30.Contains(p.Number)&&settings.IsParameterActive(p.Number)||p.Number==model.Parameters[i]).Select(p=>new ParameterOption{Number=p.Number,Name=p.Symbol+" "+p.Name}).ToList();choices.Insert(0,new ParameterOption{Number=0,Name="Резерв"});if(!choices.Any(p=>p.Number==model.Parameters[i]))choices.Add(new ParameterOption{Number=model.Parameters[i],Name="Из файла"});
                selectors[i]=Ui.Choices(overview,"Параметр "+(i+1),choices);selectors[i].SelectedItem=choices.First(p=>p.Number==model.Parameters[i]);selectors[i].IsEnabled=catalog.InputFiles.GetInt("31_32","e"+(i+1))==1;
            } Ui.Tab(tabs,"Параметры",new ScrollViewer{Content=overview});
            var grids=new DataGrid[15];var tables=new DataTable[15];
            for(int row=0;row<15;row++)
            {
                if(row==2||row==9)continue;tables[row]=Table(1,model.Lines[row].Length);for(int c=0;c<model.Lines[row].Length;c++)tables[row].Rows[0][c]=model.Lines[row][c];
                int current=row;grids[row]=new DataGrid{ItemsSource=tables[row].DefaultView};grids[row].AutoGeneratingColumn+=(s,e)=>{int c=tables[current].Columns[e.PropertyName].Ordinal;e.Column.CanUserSort=false;e.Column.IsReadOnly=current>=3&&current<=8?catalog.InputFiles.GetInt("31_32","e"+(c*2+1))!=1:current>=10&&catalog.InputFiles.GetInt("31_32","e"+(c/2*2+2))!=1;};
                Ui.Tab(tabs,"Строка "+(row+1),grids[row]);
            }
            Ui.Tab(tabs,"Справочник",Ui.Grid(catalog.Parameters,"Number","Symbol","Type","Value","Node","Name"));
            Action<string> save=target=>{Ui.Commit(tabs);model.Comment=comment.Text;for(int i=0;i<6;i++)model.Parameters[i]=((ParameterOption)selectors[i].SelectedItem).Number;for(int row=0;row<15;row++)if(tables[row]!=null)for(int c=0;c<model.Lines[row].Length;c++)model.Lines[row][c]=(double)tables[row].Rows[0][c];model.Save(target,catalog.EnginePath("inputfiles.ini"));path=target;};
            return Ui.Editor(tabs,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
        public static UIElement Pair(ApplicationSettings settings,string first,string second)
        {
            var catalog=new Unit3Model(settings.DataDirectory,settings.Language,settings.Engine);var model=InputFiles3132.Load(first,second,catalog.InputFiles,catalog.Parameters,settings.Menu.Get("files","n73","Исходный режим"));var tabs=new TabControl();
            var grid=Ui.Grid(model.Rows,"Number","Symbol","Name");grid.LoadingRow+=(s,e)=>e.Row.IsEnabled=((Input3132Row)e.Row.Item).Number!=0;Ui.Tab(tabs,"Начальные значения и пределы",grid);
            var preg=Ui.Grid(model.Preg,"Code");Ui.Tab(tabs,"PREG",preg);var panel=new StackPanel();var a=Ui.Text(panel,"Комментарий 31",model.Comment31);var b=Ui.Text(panel,"Комментарий 32",model.Comment32);var condition=Ui.Choices(panel,"Условие",catalog.Conditions.Select(c=>c.Name).ToArray());condition.SelectedIndex=model.Condition>=0&&model.Condition<condition.Items.Count?model.Condition:0;var values=model.ConditionValues.Select((v,i)=>Ui.Text(panel,"Значение "+(i+1),v.ToString(CultureInfo.CurrentCulture))).ToArray();Ui.Tab(tabs,"Условия",panel);
            Ui.Tab(tabs,"Справочник",Ui.Grid(catalog.Parameters,"Number","Symbol","Type","Value","Node","Name"));
            Action<string,string> save=(p,q)=>{Ui.Commit(tabs);model.Comment31=a.Text;model.Comment32=b.Text;model.Condition=Math.Max(0,condition.SelectedIndex);for(int i=0;i<3;i++)model.ConditionValues[i]=Parse(values[i].Text);model.Save(p,q);first=p;second=q;};
            return Ui.Editor(tabs,()=>save(first,second),()=>{var p=Ui.Save(first);if(p==null)return;var q=Ui.Save(second);if(q!=null)save(p,q);});
        }
        public sealed class ParameterOption { public int Number{get;set;} public string Name{get;set;} public override string ToString()=>Number+" "+Name; }
    }
}
