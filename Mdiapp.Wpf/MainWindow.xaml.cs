using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public partial class MainWindow : Window
    {
        private ApplicationSettings settings;
        public MainWindow() : this(null) { }
        public bool EngineEditingMode { get; }
        public MainWindow(ApplicationSettings suppliedSettings, bool engineEditingMode=false)
        {
            EngineEditingMode=engineEditingMode;
            InitializeComponent();
            if(suppliedSettings!=null){settings=suppliedSettings;RefreshStatus();}
            else Ui.Run(()=>{string programDirectory=AppDomain.CurrentDomain.BaseDirectory;CatalogWorkspace.Initialize(Path.Combine(programDirectory,"Data"),programDirectory);settings=new ApplicationSettings(programDirectory,Path.Combine(programDirectory,"models.ini"));RefreshStatus();});
            InterfaceText.Configure(settings);InterfaceText.Apply(this);
            UpdateCommands();
            PreviewKeyDown+=MainKeyDown;
            Closing+=(s,e)=>{var projects=Documents.Items.OfType<TabItem>().Select(t=>t.Content).OfType<ProjectWorkspace>().ToArray();if(projects.Any(p=>p.HasUnsavedChanges)&&MessageBox.Show(this,"Закрыть приложение? Несохранённые изменения проектов будут потеряны.","MDIAPP",MessageBoxButton.OKCancel)!=MessageBoxResult.OK){e.Cancel=true;return;}foreach(var project in projects)project.Dispose();};
            if(suppliedSettings==null)Loaded+=(s,e)=>Ui.Run(()=>{if(settings!=null&&settings.ShowUpdateNotice&&settings.UpdateNoticePath!=null&&File.Exists(settings.UpdateNoticePath))NoticeWindows.Show(settings);});
        }
        private void UpdateCommands()
        {
            bool project=(Documents.SelectedItem as TabItem)?.Content is ProjectWorkspace;
            foreach(var control in new Control[]{SaveButton,CalculateButton,SaveMenu,SaveAsMenu,PrintMenu,PreviewMenu,CalculateMenu,CancelMenu,ReadMenu,ExportMenu})control.IsEnabled=project;
            CloseMenu.IsEnabled=Documents.Items.Count>0;
        }
        private void DocumentChanged(object sender,SelectionChangedEventArgs e){if(e.Source==Documents&&SaveButton!=null)UpdateCommands();}
        private void ToggleToolbar(object sender,RoutedEventArgs e)=>MainToolbar.Visibility=((MenuItem)sender).IsChecked?Visibility.Visible:Visibility.Collapsed;
        private void ToggleStatusbar(object sender,RoutedEventArgs e)=>MainStatusbar.Visibility=((MenuItem)sender).IsChecked?Visibility.Visible:Visibility.Collapsed;
        private void PopulateWindows(object sender,RoutedEventArgs e)
        {
            if(e.Source!=WindowsMenu)return;
            WindowsMenu.Items.Clear();
            foreach(TabItem tab in Documents.Items){var item=new MenuItem{Header=tab.Header,IsCheckable=true,IsChecked=tab==Documents.SelectedItem};item.Click+=(s,args)=>Documents.SelectedItem=tab;WindowsMenu.Items.Add(item);}
            if(Documents.Items.Count==0)WindowsMenu.Items.Add(new MenuItem{Header=InterfaceText.Translate("Нет открытых окон"),IsEnabled=false});
        }
        private void MainKeyDown(object sender,System.Windows.Input.KeyEventArgs e)
        {
            if(System.Windows.Input.Keyboard.Modifiers!=System.Windows.Input.ModifierKeys.Control)return;
            if(e.Key==System.Windows.Input.Key.N){NewProject(this,e);e.Handled=true;}
            else if(e.Key==System.Windows.Input.Key.O){OpenProject(this,e);e.Handled=true;}
            else if(e.Key==System.Windows.Input.Key.S&&SaveButton.IsEnabled){SaveProject(this,e);e.Handled=true;}
        }
        private void AboutProgram(object s,RoutedEventArgs e)=>Ui.Run(()=>{var window=NoticeWindows.About(settings);window.Owner=this;window.ShowDialog();});
        private void HelpProgram(object s,RoutedEventArgs e)=>Ui.Run(()=>{string name=settings.Languages.FirstOrDefault(l=>l.Code==settings.Language)?.HelpFile;string path=Path.Combine(settings.DataDirectory,settings.Language,name??"");if(!File.Exists(path))throw new FileNotFoundException("Руководство не найдено.",path);System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path){UseShellExecute=true});});
        private void UpdateNotice(object s,RoutedEventArgs e)=>Ui.Run(()=>NoticeWindows.Show(settings));
        private void RefreshStatus()=>Status.Text="";
        private void Add(string title,UIElement content,string path=null){InterfaceText.Apply(content);var tab=new TabItem{Header=title,Content=content,Tag=path};Documents.Items.Add(tab);Documents.SelectedItem=tab;if(content is ProjectWorkspace project)project.Saved+=file=>{tab.Header=Path.GetFileName(file);tab.Tag=file;};}
        private void OpenProject(object s,RoutedEventArgs e)=>Ui.Run(()=>{string path=Ui.Open("Проект INM","INM|*.inm");if(path!=null)Add(Path.GetFileName(path),new ProjectWorkspace(settings,path),path);});
        private ProjectWorkspace ActiveProject()=>((Documents.SelectedItem as TabItem)?.Content as ProjectWorkspace)??throw new InvalidOperationException("Выберите вкладку проекта.");
        private void NewProject(object s,RoutedEventArgs e)=>Ui.Run(()=>Add("NONAME",new ProjectWorkspace(settings)));
        private void SaveProject(object s,RoutedEventArgs e)=>Ui.Run(()=>ActiveProject().Save(false));
        private void SaveProjectAs(object s,RoutedEventArgs e)=>Ui.Run(()=>ActiveProject().Save(true));
        private async void CalculateProject(object s,RoutedEventArgs e){try{await ActiveProject().Calculate();}catch(OperationCanceledException){}catch(Exception error){MessageBox.Show(error.Message,"MDIAPP",MessageBoxButton.OK,MessageBoxImage.Error);}}
        private void CancelCalculation(object s,RoutedEventArgs e)=>Ui.Run(()=>ActiveProject().Cancel());
        private void ImportResults(object s,RoutedEventArgs e)=>Ui.Run(()=>ActiveProject().LoadResults());
        private void ExportResults(object s,RoutedEventArgs e)=>Ui.Run(()=>ActiveProject().Export());
        private void PreviewProject(object s,RoutedEventArgs e)=>Ui.Run(()=>ActiveProject().PreviewPrint());
        private void PrintProject(object s,RoutedEventArgs e)=>Ui.Run(()=>ActiveProject().Print());
        private void CloseTab(object s,RoutedEventArgs e){if(Documents.SelectedItem!=null&&MessageBox.Show(this,"Закрыть вкладку? Несохранённые изменения будут потеряны.","MDIAPP",MessageBoxButton.OKCancel)==MessageBoxResult.OK){if((Documents.SelectedItem as TabItem)?.Content is ProjectWorkspace project)project.Dispose();Documents.Items.Remove(Documents.SelectedItem);}}
        private void Exit(object s,RoutedEventArgs e)=>Close();
        private void Series(object s,RoutedEventArgs e)=>Add("Unit2",Editors.Series());
        private void OpenCatalog(object s,RoutedEventArgs e)=>Ui.Run(()=>Add("Unit3",CatalogEditor.Create(settings)));
        private void CreateEngine(object s,RoutedEventArgs e)=>Ui.Run(()=>{SettingsWindows.CreateEngine(settings);RefreshStatus();});
        private void ImportEngine(object s,RoutedEventArgs e)=>Ui.Run(()=>{SettingsWindows.ImportEngine(settings);RefreshStatus();});
        private void ChooseLanguage(object s,RoutedEventArgs e)=>Ui.Run(()=>SettingsWindows.Language(settings));
        private void Engine(object s,RoutedEventArgs e)=>Ui.Run(()=>{if(EngineEditingMode){var window=CatalogEditor.CreateWindow(settings);window.Owner=this;window.ShowDialog();}else SettingsWindows.Engine(settings);RefreshStatus();});
        private void ExportSettings(object s,RoutedEventArgs e)=>Ui.Run(()=>SettingsWindows.Export(settings));
        private void ResultPath(object s,RoutedEventArgs e)=>Ui.Run(()=>ActiveProject().Export());
        private void Pair3132(object s,RoutedEventArgs e)=>Ui.Run(()=>{var p=Ui.Open("Тип 31 — начальные значения");if(p==null)return;var q=Ui.Open("Тип 32 — пределы");if(q!=null)Add("Unit11 — "+Path.GetFileName(p),Editors.Pair(settings,p,q));});
        private void File30(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Файл типа 30");if(path!=null)Add("Unit12 — "+Path.GetFileName(path),Editors.File30(settings,path));});
        private void File28(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Файл 28/281/282");if(path!=null)Add("Unit13 — "+Path.GetFileName(path),Editors.File28(settings,path));});
        private void Characteristic(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Характеристика с ветвями");if(path!=null)Add("Unit16 — "+Path.GetFileName(path),CharacteristicEditor.Create(settings,path));});
        private void ThreeColumn23(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Таблица с тремя исходными значениями (43, 46, 49)");if(path!=null)Add("Unit23 — "+Path.GetFileName(path),ThreeColumnEditor.Create(settings,path,true));});
        private void ThreeColumn(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Таблица X/Y1/Y2 (типы 42, 44, 45, 47, 48, 50)");if(path!=null)Add("Unit22 — "+Path.GetFileName(path),ThreeColumnEditor.Create(settings,path));});
        private void MultiCharacteristic(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Файл типа 1500 — семейства характеристик");if(path!=null)Add("Unit26 — "+Path.GetFileName(path),MultiCharacteristicEditor.Create(settings,path));});
        private void CorrectedCurve(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Коррекция характеристики типа 1");if(path!=null)Add("Unit27 — "+Path.GetFileName(path),CorrectedCurveEditor.Create(settings,path));});
        private void CorrectedDoubleCurve(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Коррекция двух характеристик, файл 14");if(path!=null)Add("Unit28 — "+Path.GetFileName(path),CorrectedDoubleCurveEditor.Create(settings,path));});
        private void SimpleCorrection(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Коррекция характеристики типов 11, 16, 20");if(path!=null)Add("Unit29 — "+Path.GetFileName(path),CorrectedCurveEditor.Create(settings,path,true));});
        private void CorrectedCharacteristic(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Коррекция характеристик с ветвями (3, 4, 1210–1250)");if(path!=null)Add("Unit30 — "+Path.GetFileName(path),CorrectedCharacteristicEditor.Create(settings,path));});
        private void TurbineCorrection(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Коррекция турбинных характеристик (15, 17, 19)");if(path!=null)Add("Unit31 — "+Path.GetFileName(path),CorrectedCharacteristicEditor.Create(settings,path,true));});
        private void ExtendedFile28(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Отборы с коэффициентами (28/281/282)");if(path!=null)Add("Unit32 — "+Path.GetFileName(path),ExtendedFile28Editor.Create(settings,path));});
        private void File57(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Характеристики по числу Рейнольдса, файл 57");if(path!=null)Add("Unit40 — "+Path.GetFileName(path),File57Editor.Create(settings,path));});
        private void File56(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Параметры ТП, файл 56");if(path!=null)Add("Unit39 — "+Path.GetFileName(path),File56Editor.Create(settings,path));});
        private void File55(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Отборы воздуха и мощности, файл 55");if(path!=null)Add("Unit38 — "+Path.GetFileName(path),File55Editor.Create(settings,path));});
        private void RotorSurface(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Многомерные таблицы ротора (44/47/50)");if(path!=null)Add("Unit37 — "+Path.GetFileName(path),SurfaceEditor.Create(settings,path,true));});
        private void Transition(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Переходные коэффициенты (43/46/49)");if(path!=null)Add("Unit36 — "+Path.GetFileName(path),TransitionEditor.Create(settings,path));});
        private void Surface(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Многомерные таблицы (42/45/48)");if(path!=null)Add("Unit35 — "+Path.GetFileName(path),SurfaceEditor.Create(settings,path));});
        private void File300(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Механические характеристики, файл 300");if(path!=null)Add("Unit34 — "+Path.GetFileName(path),File300Editor.Create(settings,path));});
        private void CorrectedTripleCurve(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Коррекция трёх характеристик (21/22)");if(path!=null)Add("Unit33 — "+Path.GetFileName(path),CorrectedTripleCurveEditor.Create(settings,path));});
        private void Controllers(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Регуляторы (файлы 29/40)");if(path!=null)Add("Unit25 — "+Path.GetFileName(path),ControllersEditor.Create(settings,path));});
        private void QuadCurve(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Файл типа 36 — четыре характеристики");if(path!=null)Add("Unit24 — "+Path.GetFileName(path),QuadCurveEditor.Create(settings,path));});
        private void TripleCurve(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Три характеристики (типы 21, 22, 27)");if(path!=null)Add("Unit20 — "+Path.GetFileName(path),TripleCurveEditor.Create(settings,path));});
        private void DoubleCurve(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Файл типа 14 — две характеристики");if(path!=null)Add("Unit19 — "+Path.GetFileName(path),DoubleCurveEditor.Create(settings,path));});
        private void Curve18(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Характеристика X/Y (типы 5, 8, 18, 26)");if(path!=null)Add("Unit18 — "+Path.GetFileName(path),CurveEditor.Create(settings,path,true));});
        private void Curve(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Характеристика X/Y (типы 1 и 52)");if(path!=null)Add("Unit17 — "+Path.GetFileName(path),CurveEditor.Create(settings,path));});
        private void File41(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Файл типа 41");if(path!=null)Add("Unit15 — "+Path.GetFileName(path),Editors.File41(settings,path));});
        private void File24(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Файл типа 24");if(path!=null)Add("Unit14 — "+Path.GetFileName(path),Editors.File24(settings,path));});
        private void File51(object s,RoutedEventArgs e)=>Ui.Run(()=>{var path=Ui.Open("Файл типа 51");if(path!=null)Add("Unit21 — "+Path.GetFileName(path),Editors.File51(settings,path));});
        private void Encode(object s,RoutedEventArgs e)=>ConvertFile(true);
        private void Decode(object s,RoutedEventArgs e)=>ConvertFile(false);
        private void ConvertFile(bool encode)=>Ui.Run(()=>{var p=Ui.Open("Выберите файл",encode?"Все файлы|*.*":"COD|*.cod");if(p==null)return;var q=Ui.Save(encode?p+".cod":Path.ChangeExtension(p,".txt"),"Все файлы|*.*",encode?"cod":"txt");if(q==null)return;if(string.Equals(p,q,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Выберите другое выходное имя.");using var input=File.OpenRead(p);using var memory=new MemoryStream();if(encode)LegacyCodec.Encode(input,memory);else LegacyCodec.Decode(input,memory);File.WriteAllBytes(q,memory.ToArray());});
    }
}


