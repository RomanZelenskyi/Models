using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public sealed class ProjectWorkspace : DockPanel, IDisposable
    {
        public ProjectDocument Model { get; }
        public string FilePath { get; private set; }
        public CalculationResults Results { get; private set; }
        public event Action<string> Saved;
        public bool IsCalculating => cancellation!=null;
        public bool HasUnsavedChanges { get; private set; }
        public string EngineDirectory { get=>engine.Text;set=>engine.Text=value; }
        public TabControl Pages { get; } = new TabControl();
        private readonly ApplicationSettings settings;
        private readonly Unit3Model catalog;
        private readonly List<Action> commits=new List<Action>();
        private readonly string staging=Path.Combine(Path.GetTempPath(),"Mdiapp-project-"+Guid.NewGuid().ToString("N"));
        private readonly Dictionary<string,string> stagedFiles=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        private readonly TextBox remark,code,key,engine,executable;
        private readonly TextBlock status=new TextBlock{Margin=new Thickness(8),Text="Проект открыт"};
        private readonly TabControl fileEditors=new TabControl();
        private readonly ContentControl resultContent=new ContentControl();
        private readonly StackPanel graphContent=new StackPanel();
        private readonly Button calculate;
        private CancellationTokenSource cancellation;
        private string lastBin;
        private bool cleanupRequested;
        private TextBox selectedResultIds,selectedResultRows;
        public ProjectWorkspace(ApplicationSettings settings,string path=null)
        {
            this.settings=settings;catalog=new Unit3Model(settings.DataDirectory,settings.Language,settings.Engine);FilePath=path;Model=path==null?ProjectDocument.Create(catalog):ProjectDocument.Load(path);Directory.CreateDirectory(staging);
            var bar=new WrapPanel();SetDock(bar,Dock.Top);Children.Add(bar);bar.Children.Add(Ui.Button("Сохранить",()=>Save(false)));bar.Children.Add(Ui.Button("Сохранить как…",()=>Save(true)));calculate=new Button{Content="Рассчитать"};calculate.Click+=async(s,e)=>{try{await Calculate();}catch(OperationCanceledException){status.Text="Расчёт отменён";}catch(Exception error){status.Text="Ошибка расчёта";MessageBox.Show(error.Message,"MDIAPP",MessageBoxButton.OK,MessageBoxImage.Error);}};bar.Children.Add(calculate);bar.Children.Add(Ui.Button("Остановить",()=>cancellation?.Cancel()));bar.Children.Add(Ui.Button("Прочитать результаты…",LoadResults));bar.Children.Add(Ui.Button("Экспорт…",Export));bar.Children.Add(Ui.Button("Печать…",Print));SetDock(status,Dock.Bottom);Children.Add(status);Children.Add(Pages);
            var setup=new StackPanel{Margin=new Thickness(8)};Ui.Tab(Pages,"Проект и расчёт",new ScrollViewer{Content=setup});remark=Ui.Text(setup,"Комментарий",Model.Remark);remark.Name="ProjectRemark";code=Ui.Text(setup,"Код файла",Model.FileCode);key=Ui.Text(setup,"Ключ двигателя",Model.ProjectKey.ToString());
            string sourceEngine=path!=null&&string.Equals(Path.GetFileName(Path.GetDirectoryName(path)),"datar",StringComparison.OrdinalIgnoreCase)?Directory.GetParent(Path.GetDirectoryName(path)).FullName:Path.Combine(settings.DataDirectory,settings.Engine);
            engine=Ui.Text(setup,"Папка двигателя (bin / data / datar)",sourceEngine);setup.Children.Add(Ui.Button("Выбрать папку двигателя…",()=>{var folder=Ui.Folder("Папка двигателя");if(folder!=null)engine.Text=folder;}));
            var jobs=catalog.Jobs.Where(j=>j.Enabled||j.Number==Model.Options[0]).Select(j=>new Editors.ParameterOption{Number=j.Number,Name=j.Name}).ToList();if(!jobs.Any(j=>j.Number==Model.Options[0]))jobs.Add(new Editors.ParameterOption{Number=Model.Options[0],Name="Из проекта"});var job=Ui.Choices(setup,"Задача",jobs);job.SelectedItem=jobs.First(j=>j.Number==Model.Options[0]);
            executable=Ui.Text(setup,"Расчётный EXE",catalog.Jobs.FirstOrDefault(j=>j.Number==Model.Options[0])?.Executable??"");job.SelectionChanged+=(s,e)=>{if(job.SelectedItem is Editors.ParameterOption option)executable.Text=catalog.Jobs.FirstOrDefault(j=>j.Number==option.Number)?.Executable??executable.Text;};setup.Children.Add(Ui.Button("Выбрать EXE…",()=>{var file=Ui.Open("Расчётная программа","EXE|*.exe");if(file!=null)executable.Text=file;}));
            var config=Choice(setup,"Конфигурация",catalog.Configurations,Model.Options[1]);var condition=Choice(setup,"Условия",catalog.Conditions,Model.Options[2]);var misc=catalog.Misc.Where(m=>m.Enabled).ToArray();var flags=Enumerable.Range(3,10).Select(i=>{int index=i-3;var checkbox=Ui.Check(setup,index<misc.Length?misc[index].Name:"Флаг расчёта "+(index+1),Model.Options[i]!=0);checkbox.Visibility=index<misc.Length||Model.Options[i]!=0?Visibility.Visible:Visibility.Collapsed;return checkbox;}).ToArray();var tolerances=Ui.Text(setup,"Точности (через пробел)",string.Join(" ",Model.Tolerances.Select(Number)));
            commits.Add(()=>{Model.Remark=remark.Text;Model.FileCode=code.Text;Model.ProjectKey=Int(key.Text);Model.Options[0]=((Editors.ParameterOption)job.SelectedItem).Number;Model.Options[1]=((Editors.ParameterOption)config.SelectedItem).Number;Model.Options[2]=((Editors.ParameterOption)condition.SelectedItem).Number;for(int i=0;i<10;i++)Model.Options[i+3]=flags[i].IsChecked==true?1:0;Model.Tolerances=ProjectDocument.Numbers(tolerances.Text);});
            BuildOperatingPoints();BuildFiles();BuildCorrections();Ui.Tab(Pages,"Редакторы файлов",fileEditors);Ui.Tab(Pages,"Результаты",resultContent);Ui.Tab(Pages,"Графики",new ScrollViewer{Content=graphContent});
            HasUnsavedChanges=path==null;
            AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,new TextChangedEventHandler((s,e)=>{if(IsLoaded&&Pages.SelectedIndex<5)HasUnsavedChanges=true;}));
            AddHandler(System.Windows.Controls.Primitives.ToggleButton.CheckedEvent,new RoutedEventHandler((s,e)=>{if(IsLoaded&&Pages.SelectedIndex<5)HasUnsavedChanges=true;}));
            AddHandler(System.Windows.Controls.Primitives.ToggleButton.UncheckedEvent,new RoutedEventHandler((s,e)=>{if(IsLoaded&&Pages.SelectedIndex<5)HasUnsavedChanges=true;}));
            AddHandler(ComboBox.SelectionChangedEvent,new SelectionChangedEventHandler((s,e)=>{if(IsLoaded&&Pages.SelectedIndex<5&&e.OriginalSource is ComboBox)HasUnsavedChanges=true;}));
            InterfaceText.Apply(this);
        }
        private static ComboBox Choice(Panel panel,string title,IEnumerable<Unit3Choice> definitions,int number){var choices=definitions.Where(c=>c.Enabled||c.Number==number).Select(c=>new Editors.ParameterOption{Number=c.Number,Name=c.Name}).ToList();if(!choices.Any(c=>c.Number==number))choices.Add(new Editors.ParameterOption{Number=number,Name="Из проекта"});var selector=Ui.Choices(panel,title,choices);selector.SelectedItem=choices.First(c=>c.Number==number);return selector;}
        private static int Int(string value)=>int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out int number)?number:throw new InvalidDataException("Ожидается целое число.");
        private static string Number(double value)=>value.ToString("G",CultureInfo.CurrentCulture);
        public void Commit(){if(cancellation!=null)throw new InvalidOperationException("Дождитесь окончания расчёта или остановите его.");Ui.Commit(this);foreach(var commit in commits)commit();Ui.SaveEditors(fileEditors);Model.Serialize();}
        public void Save(bool saveAs)
        {
            Commit();string target=FilePath;if(saveAs||target==null)target=Ui.Save(target??Path.Combine(engine.Text,"datar","NONAME.inm"),"INM|*.inm","inm");if(target!=null)SaveTo(target);
        }
        public void SaveTo(string target)
        {
            Commit();string previous=FilePath;var copies=Model.IncludedFiles.Where(name=>stagedFiles.ContainsKey(name)||previous==null||!string.Equals(Path.GetDirectoryName(previous),Path.GetDirectoryName(target),StringComparison.OrdinalIgnoreCase)).Select(name=>new{source=Stage(name),target=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(target),name))}).ToArray();Directory.CreateDirectory(Path.GetDirectoryName(target));Model.Save(target);foreach(var copy in copies){Directory.CreateDirectory(Path.GetDirectoryName(copy.target));string temp=copy.target+".new-"+Guid.NewGuid().ToString("N");File.Copy(copy.source,temp,true);if(File.Exists(copy.target))File.Replace(temp,copy.target,copy.target+".bak");else File.Move(temp,copy.target);}FilePath=target;status.Text="Сохранён: "+target;HasUnsavedChanges=false;Saved?.Invoke(target);
        }
        private void BuildOperatingPoints()
        {
            var panel=new DockPanel();var top=new StackPanel();SetDock(top,Dock.Top);panel.Children.Add(top);Ui.Tab(Pages,"Режимы и параметры",panel);var rows=Ui.Text(top,"Число режимов (1–10000)",Model.OperatingPoints.Count.ToString());rows.Name="ProjectRowCount";var columns=Ui.Text(top,"Число аргументов (1–30)",Model.ArgumentIds.Length.ToString());var holder=new ContentControl();panel.Children.Add(holder);Action commit=()=>{};
            Action refresh=()=>holder.Content=SurfaceEditor.Matrix(Model.OperatingPoints.Count+1,Model.ArgumentIds.Length,(r,c)=>r==0?Model.ArgumentIds[c]:Model.OperatingPoints[r-1][c],(r,c,v)=>{if(r==0){if(v!=Math.Truncate(v)||v< -1||v>2000)throw new InvalidDataException("Номер аргумента: -1–2000.");Model.ArgumentIds[c]=(int)v;}else Model.OperatingPoints[r-1][c]=v;},r=>r==0?"Номер параметра":"Режим "+r,c=>Caption(Model.ArgumentIds[c]),out commit);
            Action resize=()=>{int r=Int(rows.Text),c=Int(columns.Text);if(r<1||r>10000||c<1||c>30)throw new InvalidDataException("Режимов: 1–10000; аргументов: 1–30.");commit();var ids=Enumerable.Repeat(-1,c).ToArray();Array.Copy(Model.ArgumentIds,ids,Math.Min(c,Model.ArgumentIds.Length));Model.ArgumentIds=ids;var points=Enumerable.Range(0,r).Select(i=>Enumerable.Range(0,c).Select(j=>i<Model.OperatingPoints.Count&&j<Model.OperatingPoints[i].Length?Model.OperatingPoints[i][j]:j==0?i:-1d).ToArray()).ToArray();Model.OperatingPoints.Clear();Model.OperatingPoints.AddRange(points);refresh();};top.Children.Add(Ui.Button("Применить размеры",resize));refresh();commits.Add(()=>{if(Int(rows.Text)!=Model.OperatingPoints.Count||Int(columns.Text)!=Model.ArgumentIds.Length)resize();else commit();});
            var series=new WrapPanel();top.Children.Add(series);var target=new TextBox{Text="1",Width=60};series.Children.Add(new TextBlock{Text="Столбец для Unit2",Margin=new Thickness(8)});series.Children.Add(target);var mode=new ComboBox{ItemsSource=new[]{"Постоянное","Приращение","Степенное"},SelectedIndex=0};series.Children.Add(mode);var fields=new[]{"a","increment","k1","k2","p"}.Select(label=>{var box=new TextBox{Text="0",Width=65,ToolTip=label};series.Children.Add(box);return box;}).ToArray();series.Children.Add(Ui.Button("Применить закон Unit2",()=>{commit();int col=Int(target.Text)-1;if(col<0||col>=Model.ArgumentIds.Length)throw new InvalidDataException("Выберите существующий столбец.");var values=ParameterSeries.Calculate(Model.OperatingPoints.Select(p=>p[0]).ToArray(),(SeriesMode)mode.SelectedIndex,Parse(fields[0].Text),Parse(fields[1].Text),Parse(fields[2].Text),Parse(fields[3].Text),Parse(fields[4].Text));for(int i=0;i<values.Length;i++)Model.OperatingPoints[i][col]=values[i];refresh();}));
        }
        private static double Parse(string text)=>InputFiles3132.Parse(text,CultureInfo.CurrentCulture);
        private string Caption(int id){var p=catalog.Parameters.FirstOrDefault(v=>v.Number==id);return p==null?id.ToString():id+" "+p.Symbol;}
        private int FileType(string path){string line=File.ReadLines(path,LegacyCodec.TextEncoding).FirstOrDefault()??"";return Int(line.Trim().Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries).First());}
        private string Stage(string name)
        {
            if(stagedFiles.TryGetValue(name,out string staged))return staged;string source=FilePath==null?Path.GetFullPath(Path.Combine(engine.Text,"datar",name)):Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FilePath),name));if(!File.Exists(source))source=Path.GetFullPath(Path.Combine(engine.Text,"datar",name));string target=Path.Combine(staging,"data",Path.GetFileName(source));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);stagedFiles[name]=target;return target;
        }
        private void BuildFiles()
        {
            var panel=new DockPanel();var bar=new WrapPanel();SetDock(bar,Dock.Top);panel.Children.Add(bar);Ui.Tab(Pages,"Подключённые файлы",panel);var table=new DataTable();table.Columns.Add("Enabled",typeof(bool));table.Columns.Add("Path",typeof(string));foreach(string path in Model.IncludedFiles)table.Rows.Add(true,path);string data=Path.Combine(engine.Text,"data");if(Directory.Exists(data))foreach(string file in Directory.EnumerateFiles(data,"*.dat")){string name="..\\data\\"+Path.GetFileName(file);if(!table.Rows.Cast<DataRow>().Any(r=>string.Equals((string)r[1],name,StringComparison.OrdinalIgnoreCase)))table.Rows.Add(false,name);}var grid=new DataGrid{ItemsSource=table.DefaultView,AutoGenerateColumns=true,CanUserAddRows=false};panel.Children.Add(grid);
            commits.Add(()=>{Ui.Commit(grid);Model.IncludedFiles.Clear();Model.IncludedFiles.AddRange(table.Rows.Cast<DataRow>().Where(r=>(bool)r[0]).Select(r=>(string)r[1]));});
            bar.Children.Add(Ui.Button("Добавить DAT…",()=>{var source=Ui.Open("Подключить входной файл");if(source==null)return;string name="..\\data\\"+Path.GetFileName(source);if(table.Rows.Cast<DataRow>().Any(r=>(string)r[1]==name))throw new InvalidDataException("Файл уже подключён.");string target=Path.Combine(staging,"data",Path.GetFileName(source));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);stagedFiles[name]=target;table.Rows.Add(true,name);}));
            bar.Children.Add(Ui.Button("Удалить запись",()=>{if(grid.SelectedItem is DataRowView row)table.Rows.Remove(row.Row);}));bar.Children.Add(Ui.Button("Открыть редактор",()=>{if(!(grid.SelectedItem is DataRowView row))throw new InvalidOperationException("Выберите файл.");OpenFile((string)row[1]);}));
            bar.Children.Add(Ui.Button("Сохранить изменённые DAT в папку…",()=>{Ui.Commit(fileEditors);var directory=Ui.Folder("Папка копий входных файлов");if(directory==null)return;foreach(var pair in stagedFiles)File.Copy(pair.Value,Path.Combine(directory,Path.GetFileName(pair.Value)),true);status.Text="Копии DAT: "+directory;}));
        }
        public void OpenFile(string name)
        {
            Commit();string path=Stage(name);foreach(var tab in fileEditors.Items.OfType<TabItem>().Where(t=>Equals(t.Tag,name)).ToArray())fileEditors.Items.Remove(tab);UIElement editor=OpenEditor(ProjectSettings(),path);var item=new TabItem{Header=Path.GetFileName(path),Content=editor,Tag=name};fileEditors.Items.Add(item);fileEditors.SelectedItem=item;Pages.SelectedIndex=4;
        }
        public ApplicationSettings ProjectSettings()
        {
            return new ApplicationSettings(settings.DataDirectory,settings.PreferencesPath){ActiveParameterIds=catalog.SelectedParameters.Union(catalog.SelectedControls).Union(Model.ArgumentIds.Where(i=>i>0)).ToArray(),NodeCorrections=Model.NodeIds.Select((id,i)=>new{id,value=Model.NodeValues[i]}).ToDictionary(v=>v.id,v=>v.value)};
        }
        private UIElement OpenEditor(ApplicationSettings view,string path)
        {
            int type=FileType(path);bool newer=catalog.InputFiles.Get(type.ToString(),"new","0").Equals("True",StringComparison.OrdinalIgnoreCase)||catalog.InputFiles.Get(type.ToString(),"new","0")=="1";
            switch(type)
            {
                case 1:return newer?CorrectedCurveEditor.Create(view,path):CurveEditor.Create(view,path);case 52:return CurveEditor.Create(view,path);
                case 5:case 8:case 18:case 26:return CurveEditor.Create(view,path,true);
                case 11:case 16:case 20:if(newer)return CorrectedCurveEditor.Create(view,path,true);var curve=InputCurveFile.Load(path);return Ui.Editor(CurveEditor.CreateContent(view,curve,"n45","n46","n47",out Action commit),()=>{commit();curve.Save(path);});
                case 14:return newer?CorrectedDoubleCurveEditor.Create(view,path):DoubleCurveEditor.Create(view,path);case 21:case 22:return newer?CorrectedTripleCurveEditor.Create(view,path):TripleCurveEditor.Create(view,path);case 27:return TripleCurveEditor.Create(view,path);
                case 24:return Editors.File24(view,path);case 28:case 281:case 282:return newer?ExtendedFile28Editor.Create(view,path):Editors.File28(view,path);case 30:return Editors.File30(view,path);
                case 31:case 32:string other=Model.IncludedFiles.FirstOrDefault(name=>FileType(Stage(name))==(type==31?32:31));if(other==null)throw new InvalidDataException("В проекте отсутствует парный файл 31/32.");return Editors.Pair(view,type==31?path:Stage(other),type==32?path:Stage(other));
                case 36:return QuadCurveEditor.Create(view,path);case 29:case 40:return ControllersEditor.Create(view,path);case 41:return Editors.File41(view,path);
                case 42:case 45:case 48:return newer?SurfaceEditor.Create(view,path):ThreeColumnEditor.Create(view,path);case 44:case 47:case 50:return newer?SurfaceEditor.Create(view,path,true):ThreeColumnEditor.Create(view,path);
                case 43:case 46:case 49:return newer?TransitionEditor.Create(view,path):ThreeColumnEditor.Create(view,path,true);case 51:return Editors.File51(view,path);case 55:return File55Editor.Create(view,path);case 56:return File56Editor.Create(view,path);case 57:return File57Editor.Create(view,path);case 300:return File300Editor.Create(view,path);case 1500:return MultiCharacteristicEditor.Create(view,path);
                case 15:case 17:case 19:return newer?CorrectedCharacteristicEditor.Create(view,path,true):CharacteristicEditor.Create(view,path);
                case 3:case 4:case 1210:case 1220:case 1230:case 1240:case 1250:return newer?CorrectedCharacteristicEditor.Create(view,path):CharacteristicEditor.Create(view,path);
                default:throw new InvalidDataException("Неизвестный тип входного файла: "+type);
            }
        }
        private void BuildCorrections()
        {
            var panel=new StackPanel{Margin=new Thickness(8)};Ui.Tab(Pages,"Выходные параметры и поправки",new ScrollViewer{Content=panel});var output=Ui.Text(panel,"Выходные параметры (номера через пробел)",string.Join(" ",Model.ResultIds));output.Name="ProjectResultIds";var factors=Ui.Text(panel,"Динамические факторы (0/1 через пробел)",string.Join(" ",Model.DynamicFactors));var steps=Ui.Text(panel,"Шаги идентификации",string.Join(" ",Model.IdentificationSteps.Select(Number)));
            var controlIds=Ui.Text(panel,"Управляющие параметры",string.Join(" ",Model.ControlIds));var controls=Ui.Text(panel,"Значения управляющих параметров",string.Join(" ",Model.ControlValues.Select(Number)));var nodeIds=Ui.Text(panel,"Номера поправок узлов",string.Join(" ",Model.NodeIds));nodeIds.Name="ProjectNodeIds";var nodes=Ui.Text(panel,"Значения поправок узлов",string.Join(" ",Model.NodeValues.Select(Number)));nodes.Name="ProjectNodeValues";var correctionIds=Ui.Text(panel,"Номера многопараметрических поправок",string.Join(" ",Model.CorrectionIds));var correctionFields=Enumerable.Range(0,3).Select(i=>Ui.Text(panel,"Ряд поправок "+(i+1),string.Join(" ",Model.CorrectionValues[i].Select(Number)))).ToArray();
            commits.Add(()=>{Model.ResultIds.Clear();Model.ResultIds.AddRange(ProjectDocument.Integers(output.Text));Model.DynamicFactors=ProjectDocument.Integers(factors.Text);Model.IdentificationSteps=ProjectDocument.Numbers(steps.Text);Model.ControlIds=ProjectDocument.Integers(controlIds.Text);Model.ControlValues=ProjectDocument.Numbers(controls.Text);Model.NodeIds=ProjectDocument.Integers(nodeIds.Text);Model.NodeValues=ProjectDocument.Numbers(nodes.Text);Model.CorrectionIds=ProjectDocument.Integers(correctionIds.Text);for(int i=0;i<3;i++)Model.CorrectionValues[i]=ProjectDocument.Numbers(correctionFields[i].Text);});
        }
        public async Task Calculate()
        {
            Commit();calculate.IsEnabled=false;Pages.IsEnabled=false;cancellation=new CancellationTokenSource();var calculation=ProjectDocument.FromBytes(Model.Bytes());string enginePath=engine.Text,exeName=executable.Text;try{status.Text="Подготовка расчёта…";string root=Path.Combine(staging,"run-"+Guid.NewGuid().ToString("N"));lastBin=await Task.Run(()=>CalculationRunner.Prepare(calculation,enginePath,root));foreach(var pair in stagedFiles){string target=Path.GetFullPath(Path.Combine(lastBin,pair.Key));File.Copy(pair.Value,target,true);}string exe=Path.IsPathRooted(exeName)?exeName:Path.Combine(lastBin,exeName);status.Text="Расчёт выполняется…";int exit=await CalculationRunner.RunAsync(exe,lastBin,cancellation.Token);if(exit!=0)throw new InvalidOperationException("Расчётный EXE завершился с кодом "+exit+" (0x"+unchecked((uint)exit).ToString("X8")+"). Папка расчёта: "+lastBin);Results=CalculationRunner.Read(calculation,lastBin,catalog.InputFiles);RenderResults();status.Text="Расчёт завершён: "+Results.Rows.Count+" режимов";}finally{cancellation.Dispose();cancellation=null;calculate.IsEnabled=true;Pages.IsEnabled=true;if(cleanupRequested)Cleanup();}
        }
        public void LoadResults(){Commit();string file=Ui.Open("Результаты расчёта","Результаты|*.res;param.dat|Все файлы|*.*");if(file==null)return;Results=Path.GetExtension(file).Equals(".res",StringComparison.OrdinalIgnoreCase)?CalculationResults.LoadNative(file):CalculationResults.LoadParameters(file,new[]{Model.ArgumentIds[0]}.Concat(Model.ResultIds).ToArray());RenderResults();status.Text="Прочитаны результаты: "+Results.Rows.Count+" режимов";}
        public void SetResults(CalculationResults results){Results=results;RenderResults();}
        public CalculationResults SelectedResults()
        {
            if(Results==null)throw new InvalidOperationException("Нет результатов.");int[] ids=selectedResultIds==null||string.IsNullOrWhiteSpace(selectedResultIds.Text)?Results.ParameterIds:ProjectDocument.Integers(selectedResultIds.Text);if(ids.Any(id=>!Results.ParameterIds.Contains(id)))throw new InvalidDataException("Выбран параметр, которого нет в результатах.");ids=new[]{Results.ParameterIds[0]}.Concat(ids).Distinct().ToArray();int[] rows=selectedResultRows==null||string.IsNullOrWhiteSpace(selectedResultRows.Text)?Enumerable.Range(1,Results.Rows.Count).ToArray():ProjectDocument.Integers(selectedResultRows.Text);if(rows.Any(i=>i<1||i>Results.Rows.Count))throw new InvalidDataException("Номер режима выходит за пределы результатов.");var result=new CalculationResults(ids);foreach(int row in rows.Distinct())result.Rows.Add(ids.Select(id=>Results.Rows[row-1][Array.IndexOf(Results.ParameterIds,id)]).ToArray());return result;
        }
        private void RenderResults()
        {
            Pages.SelectedIndex=5;
            var container=new DockPanel();var filters=new StackPanel();SetDock(filters,Dock.Top);container.Children.Add(filters);selectedResultIds=Ui.Text(filters,"Выводимые параметры (номера через пробел)",string.Join(" ",Results.ParameterIds));selectedResultIds.Name="SelectedResultIds";selectedResultRows=Ui.Text(filters,"Режимы результатов (номера через пробел; пусто — все)","");selectedResultRows.Name="SelectedResultRows";var holder=new ContentControl();container.Children.Add(holder);Action render=()=>{var selection=SelectedResults();var table=new DataTable();foreach(int id in selection.ParameterIds)table.Columns.Add("P"+id,typeof(double));foreach(var row in selection.Rows)table.Rows.Add(row.Cast<object>().ToArray());var grid=new DataGrid{ItemsSource=table.DefaultView,AutoGenerateColumns=true,IsReadOnly=true};grid.AutoGeneratingColumn+=(s,e)=>{int id=Int(e.PropertyName.Substring(1));e.Column.Header=Caption(id);};holder.Content=grid;};filters.Children.Add(Ui.Button("Применить отбор",render));resultContent.Content=container;render();graphContent.Children.Clear();var choices=Results.ParameterIds.Select(id=>new Editors.ParameterOption{Number=id,Name=Caption(id)}).ToArray();var x=Ui.Choices(graphContent,"Ось X",choices);var y=Ui.Choices(graphContent,"Ось Y",choices);x.SelectedIndex=0;y.SelectedIndex=Math.Min(1,choices.Length-1);var secondary=Ui.Choices(graphContent,"Второй параметр (красный)",new[]{new Editors.ParameterOption{Number=-1,Name="Нет"}}.Concat(choices).ToArray());secondary.SelectedIndex=0;var markers=Ui.Check(graphContent,"Показывать точки",true);var plot=new CurvePlot{Height=420};graphContent.Children.Add(plot);Action redraw=()=>{if(x.SelectedItem==null||y.SelectedItem==null)return;int a=Array.IndexOf(Results.ParameterIds,((Editors.ParameterOption)x.SelectedItem).Number),b=Array.IndexOf(Results.ParameterIds,((Editors.ParameterOption)y.SelectedItem).Number);plot.XCaption=Caption(Results.ParameterIds[a]);plot.YCaption=Caption(Results.ParameterIds[b]);plot.ShowMarkers=markers.IsChecked==true;var rows=string.IsNullOrWhiteSpace(selectedResultRows.Text)?Results.Rows:ProjectDocument.Integers(selectedResultRows.Text).Where(i=>i>0&&i<=Results.Rows.Count).Select(i=>Results.Rows[i-1]).ToList();plot.Points=rows.Select(r=>new Point(r[a],r[b])).ToArray();int second=Array.IndexOf(Results.ParameterIds,((Editors.ParameterOption)secondary.SelectedItem).Number);plot.SecondaryPoints=second<0?new Point[0]:rows.Select(r=>new Point(r[a],r[second])).ToArray();plot.InvalidateVisual();};x.SelectionChanged+=(s,e)=>redraw();y.SelectionChanged+=(s,e)=>redraw();secondary.SelectionChanged+=(s,e)=>redraw();graphContent.Children.Add(Ui.Button("Сбросить масштаб",plot.ResetView));markers.Checked+=(s,e)=>redraw();markers.Unchecked+=(s,e)=>redraw();selectedResultRows.TextChanged+=(s,e)=>redraw();redraw();Pages.SelectedIndex=5;
        }
        public void Export()
        {
            Commit();if(Results==null)throw new InvalidOperationException("Нет результатов расчёта.");var panel=new StackPanel{Margin=new Thickness(12)};var window=Ui.Dialog("Экспорт результатов",panel,620,350);var format=Ui.Choices(panel,"Формат",new[]{"Текст TXT","HTML с графиками","Числовой RES"});format.SelectedIndex=0;var name=Ui.Text(panel,"Имя файла",ResultFileSelection.Suggest(FilePath??"NONAME",settings.DataDirectory,settings.Engine,ResultFileFormat.Text));format.SelectionChanged+=(s,e)=>name.Text=Path.ChangeExtension(name.Text,ResultFileSelection.Extension((ResultFileFormat)(format.SelectedIndex+1)));panel.Children.Add(Ui.Button("Выбрать файл…",()=>{var file=Ui.Save(name.Text,"Результаты|*.txt;*.htm;*.res","txt");if(file!=null)name.Text=file;}));panel.Children.Add(Ui.Button("Сохранить",()=>{var selected=(ResultFileFormat)(format.SelectedIndex+1);var options=ResultExportSettings.Load(Path.Combine(settings.DataDirectory,settings.Language,"menu.ini")).Formats[format.SelectedIndex];string directory=Path.GetDirectoryName(Path.GetFullPath(name.Text));Directory.CreateDirectory(directory);SelectedResults().Save(name.Text,selected,options,Caption,Model.Serialize(false));window.Close();status.Text="Экспорт: "+name.Text;}));window.ShowDialog();
        }
        public void Print()
        {
            Commit();var dialog=new PrintDialog();if(dialog.ShowDialog()!=true)return;var document=PrintableDocument();document.PageWidth=dialog.PrintableAreaWidth;document.PageHeight=dialog.PrintableAreaHeight;dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator,"MDIAPP — "+(FilePath??"NONAME"));
        }
        public FlowDocument PrintableDocument()
        {
            string text=Results==null||Pages.SelectedIndex<5?Model.Serialize(false):SelectedResults().Export(ResultFileFormat.Text,new ResultExportOption{Digits=8,FormatIndex=2},Caption);
            if(Pages.SelectedIndex==4&&fileEditors.SelectedItem is TabItem selected&&selected.Tag is string name)text=File.ReadAllText(Stage(name),LegacyCodec.TextEncoding);
            var document=new FlowDocument(new Paragraph(new Run((FilePath??"NONAME")+"\n"+Model.Remark+"\n\n"+text))){FontFamily=new FontFamily("Consolas"),FontSize=9,PagePadding=new Thickness(30),ColumnWidth=double.PositiveInfinity};
            if(Pages.SelectedIndex==6){var plot=graphContent.Children.OfType<CurvePlot>().FirstOrDefault();if(plot!=null&&plot.ActualWidth>0&&plot.ActualHeight>0){var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(plot.ActualWidth),(int)Math.Ceiling(plot.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(plot);bitmap.Freeze();document.Blocks.InsertBefore(document.Blocks.FirstBlock,new BlockUIContainer(new Image{Source=bitmap,Width=700,Stretch=Stretch.Uniform}));}}
            return document;
        }
        public void PreviewPrint(){Commit();var window=new Window{Title="Предварительный просмотр печати",Width=1000,Height=800,Content=new FlowDocumentReader{Document=PrintableDocument(),ViewingMode=FlowDocumentReaderViewingMode.Page}};window.ShowDialog();}
        public void Cancel()=>cancellation?.Cancel();
        public void Dispose(){cleanupRequested=true;Cancel();if(cancellation==null)Cleanup();}
        private void Cleanup(){string path=Path.GetFullPath(staging);if(Path.GetDirectoryName(path)==Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)&&Path.GetFileName(path).StartsWith("Mdiapp-project-",StringComparison.Ordinal)&&Directory.Exists(path)){try{Directory.Delete(path,true);}catch(IOException){}}}
    }
}
